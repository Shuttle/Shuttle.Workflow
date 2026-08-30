using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class AddProcessMessage(Guid processId, string typeName)
{
    public Guid ProcessId { get; } = processId;
    public string TypeName { get; } = typeName;
}

public class AddProcessMessageParticipant(IEventStore eventStore) : IParticipant<AddProcessMessage>
{
    public async Task HandleAsync(AddProcessMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        stream.Add(process.AddMessage(new(Guid.NewGuid(), message.TypeName, process.MessageCount + 1)));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}