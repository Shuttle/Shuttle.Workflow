using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Shuttle.Recall;
using Shuttle.Workflow.Events.State.v1;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.SqlServer.Models;

namespace Shuttle.Workflow.EventProcessing.v1.EventHandlers;

public class StateHandler(WorkflowDbContext dbContext) :
    IEventHandler<Registered>,
    IEventHandler<KeySet>,
    IEventHandler<ItemAdded>,
    IEventHandler<ItemValueChanged>,
    IEventHandler<ItemExpired>,
    IEventHandler<ItemRemoved>
{
    public async Task HandleAsync(IEventHandlerContext<ItemAdded> context, CancellationToken cancellationToken = default)
    {
        if (context.Event.ClosedPredecessorEffectiveDate.HasValue)
        {
            var predecessor = await GetStateItemAsync(context.PrimitiveEvent.Id, context.Event.Name, context.Event.ClosedPredecessorEffectiveDate.Value, cancellationToken);

            predecessor.EffectiveDateEnd = context.Event.EffectiveDate;
        }

        var model = new StateItem
        {
            StateId = context.PrimitiveEvent.Id,
            Name = context.Event.Name,
            Type = context.Event.Type,
            EffectiveDate = context.Event.EffectiveDate,
            EffectiveDateEnd = context.Event.EffectiveDateEnd,
            DateRegistered = context.PrimitiveEvent.RecordedAt
        };

        ApplyTypedValue(model, context.Event.Type, context.Event.Value);

        dbContext.StateItems.Add(model);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<ItemExpired> context, CancellationToken cancellationToken = default)
    {
        var model = await GetStateItemAsync(context.PrimitiveEvent.Id, context.Event.Name, context.Event.EffectiveDate, cancellationToken);

        model.EffectiveDateEnd = context.Event.EffectiveDateEnd;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<ItemRemoved> context, CancellationToken cancellationToken = default)
    {
        var model = await GetStateItemAsync(context.PrimitiveEvent.Id, context.Event.Name, context.Event.EffectiveDate, cancellationToken);

        dbContext.StateItems.Remove(model);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<ItemValueChanged> context, CancellationToken cancellationToken = default)
    {
        var model = await GetStateItemAsync(context.PrimitiveEvent.Id, context.Event.Name, context.Event.EffectiveDate, cancellationToken);

        ApplyTypedValue(model, context.Event.Type, context.Event.Value);

        model.DateRegistered = context.PrimitiveEvent.RecordedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<KeySet> context, CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(context.PrimitiveEvent.Id, cancellationToken);

        state.Key = context.Event.Key;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(IEventHandlerContext<Registered> context, CancellationToken cancellationToken = default)
    {
        dbContext.States.Add(new()
        {
            Id = context.PrimitiveEvent.Id,
            Key = context.Event.Key
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyTypedValue(StateItem model, StateItemType type, string? value)
    {
        model.StringValue = value;
        model.DateTimeValue = null;
        model.DecimalValue = null;
        model.BooleanValue = null;
        model.GuidValue = null;

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        switch (type)
        {
            case StateItemType.DateTime:
            {
                model.DateTimeValue = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                break;
            }
            case StateItemType.Decimal:
            {
                model.DecimalValue = decimal.Parse(value, CultureInfo.InvariantCulture);
                break;
            }
            case StateItemType.Boolean:
            {
                model.BooleanValue = bool.Parse(value);
                break;
            }
            case StateItemType.Guid:
            {
                model.GuidValue = Guid.Parse(value);
                break;
            }
        }
    }

    private async Task<SqlServer.Models.State> GetStateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.States.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
               ?? throw new InvalidOperationException($"State with id '{id}' has not been projected.");
    }

    private async Task<StateItem> GetStateItemAsync(Guid stateId, string name, DateTimeOffset effectiveDate, CancellationToken cancellationToken)
    {
        return await dbContext.StateItems.FirstOrDefaultAsync(item => item.StateId == stateId && item.Name == name && item.EffectiveDate == effectiveDate, cancellationToken)
               ?? throw new InvalidOperationException($"State item with state id '{stateId}', name '{name}' and effective date '{effectiveDate:O}' has not been projected.");
    }
}