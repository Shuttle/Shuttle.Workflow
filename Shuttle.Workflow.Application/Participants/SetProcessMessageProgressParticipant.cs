using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class SetProcessMessageProgress(Guid processId, Guid messageId, int? itemsTotal, int itemsCompleted)
{
    public int ItemsCompleted { get; } = itemsCompleted;
    public int? ItemsTotal { get; } = itemsTotal;
    public Guid MessageId { get; } = messageId;
    public Guid ProcessId { get; } = processId;
}

public class SetProcessMessageProgressParticipant(IEventStore eventStore) : IParticipant<SetProcessMessageProgress>
{
    public async Task HandleAsync(SetProcessMessageProgress message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.SetMessageProgress(message.MessageId, message.ItemsTotal, message.ItemsCompleted));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}
