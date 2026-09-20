using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Access.AspNetCore;
using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Application;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi.Contracts;
using Shuttle.Workflow.WebApi.Contracts.v1;
using AbandonProcess = Shuttle.Workflow.Messages.v1.AbandonProcess;
using AddProcessMessage = Shuttle.Workflow.Messages.v1.AddProcessMessage;
using CommitProcess = Shuttle.Workflow.Messages.v1.CommitProcess;
using CompleteProcessMessage = Shuttle.Workflow.Messages.v1.CompleteProcessMessage;
using ContinueProcess = Shuttle.Workflow.Messages.v1.ContinueProcess;
using DeferProcess = Shuttle.Workflow.WebApi.Contracts.v1.DeferProcess;
using FailProcess = Shuttle.Workflow.Messages.v1.FailProcess;
using RegisterProcess = Shuttle.Workflow.WebApi.Contracts.v1.RegisterProcess;
using SetProcessMessageProgress = Shuttle.Workflow.Messages.v1.SetProcessMessageProgress;
using SetProcessOverdueAt = Shuttle.Workflow.Messages.v1.SetProcessOverdueAt;

namespace Shuttle.Workflow.WebApi.Endpoints;

public static class ProcessEndpoints
{
    private static async Task<IResult> Get(IProcessQuery processQuery, IStateQuery stateQuery, Guid id)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([id])
            .IncludeMessages()
            .IncludeCommits())).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        var result = Map(process);

        var state = (await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddKey(StateKey(process.Id)).IncludeItems())).FirstOrDefault();

        if (state != null)
        {
            result.State = StateEndpoints.Map(state);
        }

        return Results.Ok(result);
    }

    private static Contracts.v1.Process Map(SqlServer.Models.Process process)
    {
        return new()
        {
            Id = process.Id,
            Key = process.Key,
            Description = process.Description,
            Name = process.Name,
            Status = process.Status,
            StatusMessage = process.StatusMessage,
            DeferredTill = process.DeferredTill,
            OverdueAt = process.OverdueAt,
            DateRegistered = process.DateRegistered,
            DateCompleted = process.DateCompleted,
            ContinuationToken = process.ContinuationToken,
            ContinuationMessageId = process.ContinuationMessageId,
            ContinuationRegisteredAt = process.ContinuationRegisteredAt,
            Commits = process.Commits.Select(item => new Contracts.v1.Process.Commit
            {
                Key = item.Key,
                DateCommitted = item.DateCommitted
            }).ToList(),
            Messages = process.Messages.Select(item => new Contracts.v1.Process.Message
            {
                Id = item.Id,
                ProcessId = process.Id,
                TypeName = item.TypeName,
                SequenceNumber = item.SequenceNumber,
                DateSent = item.DateSent,
                DateCompleted = item.DateCompleted,
                ItemsTotal = item.ItemsTotal,
                ItemsCompleted = item.ItemsCompleted
            }).ToList()
        };
    }

    private static async Task<IResult> PatchAbandon(IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, [FromBody] ProcessStatus processStatus, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        await messageDispatcher.DispatchAsync(
            () => new AbandonProcess { ProcessId = processId, Message = processStatus.Message },
            () => new Application.AbandonProcess(processId, processStatus.Message),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchContinue(IBus bus, IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        if (process.Status.Equals(Process.StatusNames.Deferred, StringComparison.OrdinalIgnoreCase) && process.DeferredTill.HasValue && process.DeferredTill.Value > DateTimeOffset.UtcNow)
        {
            return Results.BadRequest($"Process is deferred till {process.DeferredTill:O}.");
        }

        await messageDispatcher.DispatchAsync(
            () => new ContinueProcess { ProcessId = processId },
            () => new Application.ContinueProcess(processId),
            cancellationToken);

        await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchContinueDeferred(IBus bus, IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        await messageDispatcher.DispatchAsync(
            () => new ContinueProcess { ProcessId = processId },
            () => new Application.ContinueProcess(processId),
            cancellationToken);

        await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchContinueWithToken(IBus bus, IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, Guid continuationToken, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]).IncludeMessages(), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        if (!continuationToken.Equals(process.ContinuationToken))
        {
            return Results.Forbid();
        }

        if (process.Status.Equals(Process.StatusNames.Deferred, StringComparison.OrdinalIgnoreCase) && process.DeferredTill.HasValue && process.DeferredTill.Value > DateTimeOffset.UtcNow)
        {
            return Results.BadRequest($"Process is deferred till {process.DeferredTill:O}.");
        }

        var nextMessage = process.Messages.Where(item => item.DateCompleted == null).MinBy(item => item.SequenceNumber);

        if (nextMessage == null || nextMessage.Id != process.ContinuationMessageId)
        {
            return Results.BadRequest();
        }

        await messageDispatcher.DispatchAsync(
            () => new ContinueProcess { ProcessId = processId },
            () => new Application.ContinueProcess(processId),
            cancellationToken);

        await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchDefer(IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, [FromBody] DeferProcess deferProcess, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        var deferredTill = deferProcess.DeferredTill.ToUniversalTime();

        await messageDispatcher.DispatchAsync(
            () => new Messages.v1.DeferProcess { ProcessId = processId, DeferredTill = deferredTill },
            () => new Application.DeferProcess(processId, deferredTill),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchFail(IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, [FromBody] ProcessStatus processStatus, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        await messageDispatcher.DispatchAsync(
            () => new FailProcess { ProcessId = processId, Message = processStatus.Message },
            () => new Application.FailProcess(processId, processStatus.Message),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchMessageCompleted(IBus bus, MessageDispatcher messageDispatcher, Guid processId, Guid messageId, [FromBody] DeferProcess? deferProcess, CancellationToken cancellationToken)
    {
        var deferredTill = deferProcess?.DeferredTill.ToUniversalTime();

        await messageDispatcher.DispatchAsync(
            () => new CompleteProcessMessage { ProcessId = processId, MessageId = messageId, DeferredTill = deferredTill },
            () => new Application.CompleteProcessMessage(processId, messageId, deferredTill),
            cancellationToken);

        var willBeDeferred = deferredTill.HasValue && deferredTill.Value > DateTimeOffset.UtcNow;

        if (!willBeDeferred)
        {
            await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);
        }

        return Results.Accepted();
    }

    private static async Task<IResult> PatchOverdueAt(IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, [FromBody] OverdueProcess overdueProcess, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        var overdueAt = overdueProcess.OverdueAt.ToUniversalTime();

        await messageDispatcher.DispatchAsync(
            () => new SetProcessOverdueAt { ProcessId = processId, OverdueAt = overdueAt },
            () => new Application.SetProcessOverdueAt(processId, overdueAt),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchProgress(MessageDispatcher messageDispatcher, Guid processId, Guid messageId, [FromBody] ProcessMessageProgress model, CancellationToken cancellationToken)
    {
        await messageDispatcher.DispatchAsync(
            () => new SetProcessMessageProgress { ProcessId = processId, MessageId = messageId, ItemsTotal = model.ItemsTotal, ItemsCompleted = model.ItemsCompleted },
            () => new Application.SetProcessMessageProgress(processId, messageId, model.ItemsTotal, model.ItemsCompleted),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchWait(IMediator mediator, IProcessQuery processQuery, Guid processId, [FromBody] ProcessStatus processStatus, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        var waitProcess = new WaitProcess(processId, processStatus.Message);

        await mediator.SendAsync(waitProcess, cancellationToken);

        return waitProcess.HasNextMessage
            ? Results.Ok(new ProcessContinuation
            {
                Token = waitProcess.ContinuationToken,
                MessageId = waitProcess.ContinuationMessageId,
                RegisteredAt = waitProcess.ContinuationRegisteredAt
            })
            : Results.Ok();
    }

    private static async Task<IResult> Post(HttpContext httpContext, IBus bus, MessageDispatcher messageDispatcher, IProcessQuery processQuery, IProcessDefinitionQuery processDefinitionQuery, [FromBody] RegisterProcess model, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            var specification = new SqlServer.Models.Process.Specification()
                .WithKey(model.Key)
                .ExcludedStatus("Abandoned")
                .ExcludedStatus("Completed");

            var existingProcess = (await processQuery.SearchAsync(specification, cancellationToken)).FirstOrDefault();

            if (existingProcess != null)
            {
                return Results.Conflict();
            }
        }

        List<Application.RegisterProcess.MessageDefinition> messages;

        if (model.Name.Equals("."))
        {
            if (!model.Wait && !model.Messages.Any())
            {
                return Results.BadRequest("Process with name '.' must have at least one message.");
            }

            messages = model.Messages
                .OrderBy(item => item.SequenceNumber)
                .Select(item => new Application.RegisterProcess.MessageDefinition { TypeName = item.TypeName, SequenceNumber = item.SequenceNumber })
                .ToList();
        }
        else
        {
            var processDefinition = (await processDefinitionQuery.SearchAsync(new SqlServer.Models.ProcessDefinition.Specification().WithName(model.Name), cancellationToken)).FirstOrDefault();

            if (processDefinition == null)
            {
                return Results.BadRequest($"Process definition with name '{model.Name}' not found.");
            }

            foreach (var registrationState in processDefinition.StateItems)
            {
                var stateItem = model.StateItems.FirstOrDefault(item => item.Name.Equals(registrationState.Name));

                if (stateItem == null)
                {
                    return Results.BadRequest($"State item with name '{registrationState.Name}' not found in registration message.");
                }

                var processStateItemType = Enum.Parse<StateItemType>(stateItem.Type, true);

                if (processStateItemType != registrationState.Type)
                {
                    return Results.BadRequest($"State item with name '{registrationState.Name}' has a type '{processStateItemType}' that does not match the expected type '{registrationState.Type}'.");
                }
            }

            messages = processDefinition.Messages
                .OrderBy(item => item.SequenceNumber)
                .Select(item => new Application.RegisterProcess.MessageDefinition { TypeName = item.TypeName, SequenceNumber = item.SequenceNumber, InvokeTimeout = item.InvokeTimeout })
                .ToList();
        }

        var processId = Guid.NewGuid();
        var stateId = Guid.NewGuid();

        var stateItems = model.StateItems
            .Select(item => new Application.RegisterProcess.StateItemValue { Name = item.Name, Value = item.Value, Type = Enum.Parse<StateItemType>(item.Type, true), EffectiveDate = item.EffectiveDate })
            .ToList();

        await messageDispatcher.DispatchAsync(
            () => new Messages.v1.RegisterProcess
            {
                Id = processId,
                StateId = stateId,
                Name = model.Name,
                Key = model.Key,
                Description = model.Description,
                DeferredTill = model.DeferredTill,
                OverdueAt = model.OverdueAt,
                Messages = messages.Select(item => new Messages.v1.RegisterProcess.MessageDefinition { TypeName = item.TypeName, SequenceNumber = item.SequenceNumber, InvokeTimeout = item.InvokeTimeout }).ToList(),
                StateItems = stateItems.Select(item => new StateItemValue { Name = item.Name, Value = item.Value, Type = item.Type.ToString(), EffectiveDate = item.EffectiveDate }).ToList()
            },
            () => new Application.RegisterProcess(processId, stateId, model.Name, model.Key, model.Description, model.DeferredTill, model.OverdueAt, messages, stateItems),
            cancellationToken);

        if (!model.Wait)
        {
            await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);
        }

        return Results.Accepted($"{httpContext.Request.Scheme}://{httpContext.Request.Host}/v1/processes/{processId}", new Registered { Id = processId });
    }

    private static async Task<IResult> PostCommit(MessageDispatcher messageDispatcher, IProcessQuery processQuery, Guid processId, RegisterCommit commit, CancellationToken cancellationToken)
    {
        if (!(await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).Any())
        {
            return Results.NotFound();
        }

        var dateCommitted = commit.DateCommitted?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

        await messageDispatcher.DispatchAsync(
            () => new CommitProcess { ProcessId = processId, Key = commit.Key, DateCommitted = dateCommitted },
            () => new Application.CommitProcess(processId, commit.Key, dateCommitted),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PostMessage(IProcessQuery processQuery, MessageDispatcher messageDispatcher, Guid processId, [FromBody] Contracts.v1.Process.Message message, CancellationToken cancellationToken)
    {
        var process = (await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().AddIds([processId]), cancellationToken)).FirstOrDefault();

        if (process == null)
        {
            return Results.NotFound();
        }

        if (process.DateCompleted.HasValue)
        {
            return Results.BadRequest("Process has already completed.");
        }

        await messageDispatcher.DispatchAsync(
            () => new AddProcessMessage { ProcessId = processId, TypeName = message.TypeName },
            () => new Application.AddProcessMessage(processId, message.TypeName),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PostSearch(IProcessQuery processQuery, IStateQuery stateQuery, [FromBody] Contracts.v1.Process.Specification model)
    {
        var specification = new SqlServer.Models.Process.Specification().AddIds(model.Ids);

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            specification.WithName(model.Name);
        }

        if (!string.IsNullOrWhiteSpace(model.NameMatch))
        {
            specification.WithNameMatch(model.NameMatch);
        }

        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            specification.WithKey(model.Key);
        }

        if (!string.IsNullOrWhiteSpace(model.KeyMatch))
        {
            specification.WithKeyMatch(model.KeyMatch);
        }

        if (model.ActiveOnly.HasValue && model.ActiveOnly.Value)
        {
            specification.ActiveOnly();
        }

        foreach (var status in model.IncludedStatuses)
        {
            specification.IncludedStatus(status);
        }

        foreach (var status in model.ExcludedStatuses)
        {
            specification.ExcludedStatus(status);
        }

        if (model.ShouldIncludeMessages)
        {
            specification.IncludeMessages();
        }

        if (model.ShouldIncludeCommits)
        {
            specification.IncludeCommits();
        }

        if (model.FromDateCompletedInclusive.HasValue)
        {
            specification.WithFromDateCompletedInclusive(model.FromDateCompletedInclusive.Value);
        }

        if (model.ToDateCompletedExclusive.HasValue)
        {
            specification.WithToDateCompletedExclusive(model.ToDateCompletedExclusive.Value);
        }

        if (model.FromDateRegisteredInclusive.HasValue)
        {
            specification.WithFromDateRegisteredInclusive(model.FromDateRegisteredInclusive.Value);
        }

        if (model.ToDateRegisteredExclusive.HasValue)
        {
            specification.WithToDateRegisteredExclusive(model.ToDateRegisteredExclusive.Value);
        }

        if (model.MaximumRows > 0)
        {
            specification.WithMaximumRows(model.MaximumRows);
        }

        var result = (await processQuery.SearchAsync(specification)).Select(Map).ToList();

        var states = (await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddKeys(result.Select(item => StateKey(item.Id))).IncludeItems())).ToList();

        foreach (var process in result)
        {
            var state = states.FirstOrDefault(item => item.Key == StateKey(process.Id));

            if (state != null)
            {
                process.State = StateEndpoints.Map(state);
            }
        }

        return Results.Ok(result);
    }

    public static string StateKey(Guid processId)
    {
        return $"[process]:{processId}";
    }

    extension(WebApplication app)
    {
        public WebApplication MapProcessEndpoints(ApiVersionSet versionSet)
        {
            var apiVersion1 = new ApiVersion(1, 0);

            app.MapGet("/v{version:apiVersion}/processes/{id:Guid}", Get)
                .RequirePermission(Permissions.Processes.View)
                .WithName("GetProcess")
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/processes", Post)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/processes/{processId:Guid}/messages", PostMessage)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/wait", PatchWait)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/defer", PatchDefer)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/overdue-at", PatchOverdueAt)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/abandon", PatchAbandon)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/fail", PatchFail)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/continue", PatchContinue)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/continue/{continuationToken:Guid}", PatchContinueWithToken)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/continue-deferred", PatchContinueDeferred)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/message-completed/{messageId:Guid}", PatchMessageCompleted)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/processes/{processId:Guid}/messages/{messageId:Guid}/progress", PatchProgress)
                .RequirePermission(Permissions.Processes.Manage)
                .WithName("SetProgress")
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/processes/{processId:Guid}/commits", PostCommit)
                .RequirePermission(Permissions.Processes.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/processes/search", PostSearch)
                .RequirePermission(Permissions.Processes.View)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            return app;
        }
    }
}