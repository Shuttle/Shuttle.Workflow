using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class AddProcessDefinitionMessage(Guid processDefinitionId, string typeName, int sequenceNumber, TimeSpan? invokeTimeout)
{
    public TimeSpan? InvokeTimeout { get; } = invokeTimeout;
    public Guid ProcessDefinitionId { get; } = processDefinitionId;
    public int SequenceNumber { get; } = sequenceNumber;
    public string TypeName { get; } = typeName;
}

public class AddProcessDefinitionMessageParticipant(IEventStore eventStore) : IParticipant<AddProcessDefinitionMessage>
{
    public async Task HandleAsync(AddProcessDefinitionMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessDefinitionId, cancellationToken: cancellationToken);
        var processDefinition = stream.Get<ProcessDefinition>();

        stream.Add(processDefinition.AddMessage(message.TypeName, message.SequenceNumber, message.InvokeTimeout));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}