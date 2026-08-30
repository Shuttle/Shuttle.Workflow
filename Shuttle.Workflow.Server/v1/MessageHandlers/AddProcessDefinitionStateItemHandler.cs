using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class AddProcessDefinitionStateItemHandler(IMediator mediator) : IMessageHandler<AddProcessDefinitionStateItem>
{
    public async Task HandleAsync(AddProcessDefinitionStateItem message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.AddProcessDefinitionStateItem(message.ProcessDefinitionId, message.Name, Enum.Parse<StateItemType>(message.Type, true)), cancellationToken);
    }
}