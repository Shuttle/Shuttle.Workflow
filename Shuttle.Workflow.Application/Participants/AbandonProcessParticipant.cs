using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class AbandonProcess(Guid processId, string message)
{
    public string Message { get; } = message;
    public Guid ProcessId { get; } = processId;
}

public class AbandonProcessParticipant(IEventStore eventStore) : IParticipant<AbandonProcess>
{
    public async Task HandleAsync(AbandonProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.Abandon(message.Message));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}