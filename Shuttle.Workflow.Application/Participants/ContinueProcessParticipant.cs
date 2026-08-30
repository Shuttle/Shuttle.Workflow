using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class ContinueProcess(Guid processId)
{
    public Guid ProcessId { get; } = processId;
}

public class ContinueProcessParticipant(IEventStore eventStore) : IParticipant<ContinueProcess>
{
    public async Task HandleAsync(ContinueProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.Continue());

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}