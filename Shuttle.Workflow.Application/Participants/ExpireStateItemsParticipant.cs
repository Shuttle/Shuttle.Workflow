using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class ExpireStateItems(Guid stateId, List<ExpireStateItems.Expiry> expiries)
{
    public List<Expiry> Expiries { get; } = expiries;
    public Guid StateId { get; } = stateId;

    public class Expiry
    {
        public DateTimeOffset EffectiveDateEnd { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

public class ExpireStateItemsParticipant(IEventStore eventStore) : IParticipant<ExpireStateItems>
{
    public async Task HandleAsync(ExpireStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.StateId, cancellationToken: cancellationToken);
        var state = stream.Get<State>();

        foreach (var expiry in message.Expiries)
        {
            var itemExpired = state.ExpireItem(expiry.Name, expiry.EffectiveDateEnd);

            if (itemExpired != null)
            {
                stream.Add(itemExpired);
            }
        }

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}