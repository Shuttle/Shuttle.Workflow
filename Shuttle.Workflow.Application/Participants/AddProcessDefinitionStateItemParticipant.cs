using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class AddProcessDefinitionStateItem(Guid processDefinitionId, string name, StateItemType type)
{
    public string Name { get; } = name;
    public Guid ProcessDefinitionId { get; } = processDefinitionId;
    public StateItemType Type { get; } = type;
}

public class AddProcessDefinitionStateItemParticipant(IEventStore eventStore) : IParticipant<AddProcessDefinitionStateItem>
{
    public async Task HandleAsync(AddProcessDefinitionStateItem message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = await eventStore.GetAsync(message.ProcessDefinitionId, cancellationToken: cancellationToken);
        var processDefinition = stream.Get<ProcessDefinition>();

        stream.Add(processDefinition.AddStateItem(message.Name, message.Type));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}