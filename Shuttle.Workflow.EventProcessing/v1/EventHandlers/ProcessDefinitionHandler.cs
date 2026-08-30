using Microsoft.EntityFrameworkCore;
using Shuttle.Recall;
using Shuttle.Workflow.Events.ProcessDefinition.v1;
using Shuttle.Workflow.SqlServer;

namespace Shuttle.Workflow.EventProcessing.v1.EventHandlers;

public class ProcessDefinitionHandler(WorkflowDbContext dbContext) :
    IEventHandler<Registered>,
    IEventHandler<StateItemAdded>,
    IEventHandler<MessageAdded>
{
    public async Task HandleAsync(IEventHandlerContext<MessageAdded> context, CancellationToken cancellationToken = default)
    {
        _ = await GetProcessDefinitionAsync(context.PrimitiveEvent.Id, cancellationToken);

        foreach (var resequencedMessage in context.Event.Resequenced)
        {
            var existing = await dbContext.ProcessDefinitionMessages.FirstOrDefaultAsync(item => item.ProcessDefinitionId == context.PrimitiveEvent.Id && item.TypeName == resequencedMessage.TypeName, cancellationToken)
                           ?? throw new InvalidOperationException($"Process definition message with type name '{resequencedMessage.TypeName}' has not been projected.");

            existing.SequenceNumber = resequencedMessage.SequenceNumber;
        }

        dbContext.ProcessDefinitionMessages.Add(new()
        {
            ProcessDefinitionId = context.PrimitiveEvent.Id,
            TypeName = context.Event.TypeName,
            SequenceNumber = context.Event.SequenceNumber,
            InvokeTimeout = context.Event.InvokeTimeout
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Registered> context, CancellationToken cancellationToken = default)
    {
        dbContext.ProcessDefinitions.Add(new()
        {
            Id = context.PrimitiveEvent.Id,
            Name = context.Event.Name,
            Description = context.Event.Description
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<StateItemAdded> context, CancellationToken cancellationToken = default)
    {
        dbContext.ProcessDefinitionStateItems.Add(new()
        {
            ProcessDefinitionId = context.PrimitiveEvent.Id,
            Name = context.Event.Name,
            Type = context.Event.Type
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SqlServer.Models.ProcessDefinition> GetProcessDefinitionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ProcessDefinitions.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
               ?? throw new InvalidOperationException($"Process definition with id '{id}' has not been projected.");
    }
}