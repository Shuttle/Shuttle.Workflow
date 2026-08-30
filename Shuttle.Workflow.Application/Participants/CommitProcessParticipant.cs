using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class CommitProcess(Guid processId, string key, DateTimeOffset dateCommitted)
{
    public DateTimeOffset DateCommitted { get; } = dateCommitted;
    public string Key { get; } = key;
    public Guid ProcessId { get; } = processId;
}

public class CommitProcessParticipant(IEventStore eventStore) : IParticipant<CommitProcess>
{
    public async Task HandleAsync(CommitProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        var committed = process.Commit(message.Key, message.DateCommitted);

        if (committed == null)
        {
            return;
        }

        stream.Add(committed);

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}