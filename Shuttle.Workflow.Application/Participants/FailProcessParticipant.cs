using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class FailProcess(Guid processId, string message)
{
    public string Message { get; } = message;
    public Guid ProcessId { get; } = processId;
}

public class FailProcessParticipant(IEventStore eventStore) : IParticipant<FailProcess>
{
    public async Task HandleAsync(FailProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.Fail(message.Message));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}