using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class WaitProcess(Guid processId, string message)
{
    public Guid ContinuationMessageId { get; private set; }
    public DateTimeOffset ContinuationRegisteredAt { get; private set; }
    public Guid ContinuationToken { get; private set; }
    public bool HasNextMessage { get; private set; }
    public string Message { get; } = message;
    public Guid ProcessId { get; } = processId;

    public WaitProcess WithContinuation(Guid token, Guid messageId, DateTimeOffset registeredAt)
    {
        HasNextMessage = true;
        ContinuationToken = token;
        ContinuationMessageId = messageId;
        ContinuationRegisteredAt = registeredAt;

        return this;
    }
}

public class WaitProcessParticipant(IEventStore eventStore) : IParticipant<WaitProcess>
{
    public async Task HandleAsync(WaitProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        var nextMessage = process.GetNextMessage();

        if (nextMessage == null)
        {
            stream.Add(process.Complete());
        }
        else
        {
            stream.Add(process.Wait(message.Message));
            stream.Add(process.WithContinuation());

            message.WithContinuation(process.ContinuationToken!.Value, process.ContinuationMessageId!.Value, process.ContinuationRegisteredAt!.Value);
        }

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}