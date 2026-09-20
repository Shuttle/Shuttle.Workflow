using Microsoft.EntityFrameworkCore;
using Shuttle.Hopper;
using Shuttle.Recall;
using Shuttle.Workflow.Events.Process.v1;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.SqlServer.Models;
using Started = Shuttle.Workflow.Events.Process.v1.Started;

namespace Shuttle.Workflow.EventProcessing.v1.EventHandlers;

public class ProcessHandler(WorkflowDbContext dbContext, IBus bus) :
    IEventHandler<Registered>,
    IEventHandler<Deferred>,
    IEventHandler<OverdueAtSet>,
    IEventHandler<Started>,
    IEventHandler<Completed>,
    IEventHandler<Waited>,
    IEventHandler<Failed>,
    IEventHandler<Abandoned>,
    IEventHandler<MessageAdded>,
    IEventHandler<MessageSent>,
    IEventHandler<MessageCompleted>,
    IEventHandler<MessageProgressSet>,
    IEventHandler<ContinuationSet>,
    IEventHandler<Continued>,
    IEventHandler<Committed>
{
    public async Task HandleAsync(IEventHandlerContext<Abandoned> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.Status = Process.StatusNames.Abandoned;
        process.StatusMessage = context.Event.Message;
        process.DateCompleted = context.Event.DateCompleted;

        await dbContext.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new ProcessAbandoned
        {
            Id = context.PrimitiveEvent.Id,
            Message = context.Event.Message,
            DateCompleted = context.Event.DateCompleted
        }, cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Committed> context, CancellationToken cancellationToken = default)
    {
        dbContext.ProcessCommits.Add(new()
        {
            ProcessId = context.PrimitiveEvent.Id,
            Key = context.Event.Key,
            DateCommitted = context.Event.DateCommitted
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Completed> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.Status = Process.StatusNames.Completed;
        process.DateCompleted = context.Event.DateCompleted;

        await dbContext.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new ProcessCompleted
        {
            Id = context.PrimitiveEvent.Id,
            DateCompleted = context.Event.DateCompleted
        }, cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<ContinuationSet> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.ContinuationToken = context.Event.Token;
        process.ContinuationMessageId = context.Event.MessageId;
        process.ContinuationRegisteredAt = context.Event.RegisteredAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Continued> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.DeferredTill = null;
        process.Status = Process.StatusNames.Started;
        process.StatusMessage = context.Event.StatusMessage;
        process.ContinuationToken = null;
        process.ContinuationMessageId = null;
        process.ContinuationRegisteredAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Deferred> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.DeferredTill = context.Event.DeferredTill;
        process.Status = Process.StatusNames.Deferred;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Failed> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.Status = Process.StatusNames.Failed;
        process.StatusMessage = context.Event.Message;
        process.DateCompleted = context.Event.DateCompleted;

        await dbContext.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new ProcessFailed
        {
            Id = context.PrimitiveEvent.Id,
            Message = context.Event.Message,
            DateCompleted = context.Event.DateCompleted
        }, cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<MessageAdded> context, CancellationToken cancellationToken = default)
    {
        dbContext.ProcessMessages.Add(new()
        {
            Id = context.Event.MessageId,
            ProcessId = context.PrimitiveEvent.Id,
            TypeName = context.Event.TypeName,
            SequenceNumber = context.Event.SequenceNumber,
            InvokeTimeout = context.Event.InvokeTimeout
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<MessageCompleted> context, CancellationToken cancellationToken = default)
    {
        var message = await GetProcessMessageAsync(context.Event.MessageId, cancellationToken);

        message.DateCompleted = context.Event.DateCompleted;

        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.ContinuationToken = null;
        process.ContinuationMessageId = null;
        process.ContinuationRegisteredAt = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<MessageProgressSet> context, CancellationToken cancellationToken = default)
    {
        var message = await GetProcessMessageAsync(context.Event.MessageId, cancellationToken);

        message.ItemsTotal = context.Event.ItemsTotal;
        message.ItemsCompleted = context.Event.ItemsCompleted;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<MessageSent> context, CancellationToken cancellationToken = default)
    {
        var message = await GetProcessMessageAsync(context.Event.MessageId, cancellationToken);

        message.DateSent = context.Event.DateSent;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<OverdueAtSet> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.OverdueAt = context.Event.OverdueAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Registered> context, CancellationToken cancellationToken = default)
    {
        dbContext.Processes.Add(new()
        {
            Id = context.PrimitiveEvent.Id,
            Name = context.Event.Name,
            Key = context.Event.Key,
            Description = context.Event.Description,
            DateRegistered = context.Event.DateRegistered,
            Status = Process.StatusNames.Registered
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new ProcessRegistered
        {
            Id = context.PrimitiveEvent.Id,
            Name = context.Event.Name,
            Key = context.Event.Key
        }, cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Started> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.Status = Process.StatusNames.Started;
        process.StatusMessage = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Waited> context, CancellationToken cancellationToken = default)
    {
        var process = await GetProcessAsync(context.PrimitiveEvent.Id, cancellationToken);

        process.Status = Process.StatusNames.Waiting;
        process.StatusMessage = context.Event.Message;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SqlServer.Models.Process> GetProcessAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Processes.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
               ?? throw new InvalidOperationException($"Process with id '{id}' has not been projected.");
    }

    private async Task<ProcessMessage> GetProcessMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ProcessMessages.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
               ?? throw new InvalidOperationException($"Process message with id '{id}' has not been projected.");
    }
}