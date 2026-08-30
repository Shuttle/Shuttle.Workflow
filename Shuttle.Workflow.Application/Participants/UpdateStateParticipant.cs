using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class UpdateState(Guid id, string key, List<RegisterState.StateItemValue> items)
{
    public Guid Id { get; } = id;
    public List<RegisterState.StateItemValue> Items { get; } = items;
    public string Key { get; } = key;
}

public class UpdateStateParticipant(IEventStore eventStore) : IParticipant<UpdateState>
{
    public async Task HandleAsync(UpdateState message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.Id, cancellationToken: cancellationToken);
        var state = stream.Get<State>();

        stream.Add(state.WithKey(message.Key));

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