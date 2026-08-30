using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class RegisterProcessDefinition(Guid id, string name, string description)
{
    public string Description { get; } = description;
    public Guid Id { get; } = id;
    public string Name { get; } = name;
}

public class RegisterProcessDefinitionParticipant(IEventStore eventStore) : IParticipant<RegisterProcessDefinition>
{
    public async Task HandleAsync(RegisterProcessDefinition message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var stream = (await eventStore.GetAsync(message.Id, cancellationToken: cancellationToken)).MustBeEmpty();
        var processDefinition = stream.Get<ProcessDefinition>();

        stream.Add(processDefinition.Register(message.Name, message.Description));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
    }
}