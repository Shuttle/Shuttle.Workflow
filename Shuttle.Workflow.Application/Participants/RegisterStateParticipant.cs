using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class RegisterState(Guid id, string key, List<RegisterState.StateItemValue> items)
{
    public Guid Id { get; } = id;
    public List<StateItemValue> Items { get; } = items;
    public string Key { get; } = key;

    public class StateItemValue
    {
        public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UnixEpoch;
        public DateTimeOffset EffectiveDateEnd { get; set; } = DateTimeOffset.MaxValue;
        public string Name { get; set; } = string.Empty;
        public StateItemType Type { get; set; }
        public string? Value { get; set; }
    }
}

public class RegisterStateParticipant(IEventStore eventStore) : IParticipant<RegisterState>
{
    public async Task HandleAsync(RegisterState message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = (await eventStore.GetAsync(message.Id, cancellationToken: cancellationToken)).MustBeEmpty();
        var state = stream.Get<State>();

        stream.Add(state.Register(message.Key));

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