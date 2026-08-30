using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class RemoveStateItems(Guid stateId, List<string> names)
{
    public List<string> Names { get; } = names;
    public Guid StateId { get; } = stateId;
}

public class RemoveStateItemsParticipant(IEventStore eventStore) : IParticipant<RemoveStateItems>
{
    public async Task HandleAsync(RemoveStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.StateId, cancellationToken: cancellationToken);
        var state = stream.Get<State>();

        foreach (var name in message.Names)
        {
            foreach (var itemRemoved in state.RemoveItem(name))
            {
                stream.Add(itemRemoved);
            }
        }

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}