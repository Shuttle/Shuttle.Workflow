using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class CompleteProcessMessage(Guid processId, Guid messageId, DateTimeOffset? deferredTill)
{
    public DateTimeOffset? DeferredTill { get; } = deferredTill;
    public Guid MessageId { get; } = messageId;
    public Guid ProcessId { get; } = processId;
}

public class CompleteProcessMessageParticipant(IEventStore eventStore) : IParticipant<CompleteProcessMessage>
{
    public async Task HandleAsync(CompleteProcessMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.CompleteMessage(message.MessageId));

        if (message.DeferredTill.HasValue)
        {
            var deferred = process.Defer(message.DeferredTill.Value);

            if (deferred != null)
            {
                stream.Add(deferred);
            }
        }

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}