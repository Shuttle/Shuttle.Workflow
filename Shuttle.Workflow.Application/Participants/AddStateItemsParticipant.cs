using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class AddStateItems(Guid stateId, List<RegisterState.StateItemValue> items)
{
    public List<RegisterState.StateItemValue> Items { get; } = items;
    public Guid StateId { get; } = stateId;
}

public class AddStateItemsParticipant(IEventStore eventStore) : IParticipant<AddStateItems>
{
    public async Task HandleAsync(AddStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.StateId, cancellationToken: cancellationToken);
        var state = stream.Get<State>();

        foreach (var item in message.Items)
        {
            var itemAdded = state.AddItem(new(item.Name, item.Value, item.Type, item.EffectiveDate, item.EffectiveDateEnd));

            if (itemAdded != null)
            {
                stream.Add(itemAdded);
            }
        }

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}