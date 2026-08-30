using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class DeferProcess(Guid processId, DateTimeOffset deferredTill)
{
    public DateTimeOffset DeferredTill { get; } = deferredTill;
    public Guid ProcessId { get; } = processId;
}

public class DeferProcessParticipant(IEventStore eventStore) : IParticipant<DeferProcess>
{
    public async Task HandleAsync(DeferProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        var deferred = process.Defer(message.DeferredTill);

        if (deferred == null)
        {
            return;
        }

        stream.Add(deferred);

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}