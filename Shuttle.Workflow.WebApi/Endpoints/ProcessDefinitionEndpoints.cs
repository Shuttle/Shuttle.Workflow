using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Access.AspNetCore;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi.Contracts;

namespace Shuttle.Workflow.WebApi.Endpoints;

public static class ProcessDefinitionEndpoints
{
    private static Contracts.v1.ProcessDefinition Map(SqlServer.Models.ProcessDefinition processDefinition)
    {
        return new()
        {
            Id = processDefinition.Id,
            Description = processDefinition.Description,
            Name = processDefinition.Name,
            StateItems = processDefinition.StateItems.Select(item => new Contracts.v1.ProcessDefinition.StateItem
            {
                Name = item.Name,
                Type = item.Type.ToString()
            }).ToList(),
            Messages = processDefinition.Messages.Select(item => new Contracts.v1.ProcessDefinition.Message
            {
                TypeName = item.TypeName,
                SequenceNumber = item.SequenceNumber
            }).ToList()
        };
    }

    private static async Task<IResult> Post(IProcessDefinitionQuery processDefinitionQuery, MessageDispatcher messageDispatcher, [FromBody] Contracts.v1.ProcessDefinition model, CancellationToken cancellationToken)
    {
        if ((await processDefinitionQuery.SearchAsync(new SqlServer.Models.ProcessDefinition.Specification().WithName(model.Name), cancellationToken)).Any())
        {
            return Results.Conflict($"A process definition with name '{model.Name}' already exists.");
        }

        var id = Guid.NewGuid();

        await messageDispatcher.DispatchAsync(
            () => new RegisterProcessDefinition { Id = id, Name = model.Name, Description = model.Description },
            () => new Application.RegisterProcessDefinition(id, model.Name, model.Description),
            cancellationToken);

        return Results.Accepted(null, new Registered { Id = id });
    }

    private static async Task<IResult> PostMessage(IProcessDefinitionQuery processDefinitionQuery, MessageDispatcher messageDispatcher, Guid processDefinitionId, [FromBody] Contracts.v1.ProcessDefinition.Message model, CancellationToken cancellationToken)
    {
        if (!await processDefinitionQuery.ContainsAsync(new SqlServer.Models.ProcessDefinition.Specification().WithId(processDefinitionId), cancellationToken))
        {
            return Results.NotFound();
        }

        await messageDispatcher.DispatchAsync(
            () => new AddProcessDefinitionMessage { ProcessDefinitionId = processDefinitionId, TypeName = model.TypeName, SequenceNumber = model.SequenceNumber },
            () => new Application.AddProcessDefinitionMessage(processDefinitionId, model.TypeName, model.SequenceNumber, null),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PostSearch(IProcessDefinitionQuery processDefinitionQuery, IStateQuery stateQuery, [FromBody] Contracts.v1.ProcessDefinition.Specification model)
    {
        var specification = new SqlServer.Models.ProcessDefinition.Specification().WithMaximumRows(model.MaximumRows);

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            specification.WithName(model.Name);
        }

        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            specification.WithName(model.Name);
        }

        return Results.Ok((await processDefinitionQuery.SearchAsync(specification)).Select(Map).ToList());
    }

    private static async Task<IResult> PostStateItem(IProcessDefinitionQuery processDefinitionQuery, MessageDispatcher messageDispatcher, Guid processDefinitionId, [FromBody] Contracts.v1.ProcessDefinition.StateItem model, CancellationToken cancellationToken)
    {
        if (!await processDefinitionQuery.ContainsAsync(new SqlServer.Models.ProcessDefinition.Specification().WithId(processDefinitionId), cancellationToken))
        {
            return Results.NotFound();
        }

        if (!Enum.TryParse<StateItemType>(model.Type, true, out var type))
        {
            return Results.BadRequest($"'{model.Type}' is not a valid state item type.");
        }

        await messageDispatcher.DispatchAsync(
            () => new AddProcessDefinitionStateItem { ProcessDefinitionId = processDefinitionId, Name = model.Name, Type = type.ToString() },
            () => new Application.AddProcessDefinitionStateItem(processDefinitionId, model.Name, type),
            cancellationToken);

        return Results.Accepted();
    }

    extension(WebApplication app)
    {
        public WebApplication MapProcessDefinitionEndpoints(ApiVersionSet versionSet)
        {
            var apiVersion1 = new ApiVersion(1, 0);

            app.MapPost("/v{version:apiVersion}/process-definitions/search", PostSearch)
                .RequirePermission(Permissions.ProcessDefinitions.View)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/process-definitions", Post)
                .RequirePermission(Permissions.ProcessDefinitions.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/process-definitions/{processDefinitionId:Guid}/state-items", PostStateItem)
                .RequirePermission(Permissions.ProcessDefinitions.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/process-definitions/{processDefinitionId:Guid}/messages", PostMessage)
                .RequirePermission(Permissions.ProcessDefinitions.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            return app;
        }
    }
}