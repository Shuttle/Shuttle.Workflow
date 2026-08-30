using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class RegisterProcessDefinitionHandler(IMediator mediator) : IMessageHandler<RegisterProcessDefinition>
{
    public async Task HandleAsync(RegisterProcessDefinition message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.RegisterProcessDefinition(message.Id, message.Name, message.Description), cancellationToken);
    }
}