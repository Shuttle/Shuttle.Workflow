using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class SetProcessOverdueAt(Guid processId, DateTimeOffset overdueAt)
{
    public DateTimeOffset OverdueAt { get; } = overdueAt;
    public Guid ProcessId { get; } = processId;
}

public class SetProcessOverdueAtParticipant(IEventStore eventStore) : IParticipant<SetProcessOverdueAt>
{
    public async Task HandleAsync(SetProcessOverdueAt message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.WithOverdueAt(message.OverdueAt));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}