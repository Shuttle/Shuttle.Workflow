using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class UpdateStateHandler(IMediator mediator) : IMessageHandler<UpdateState>
{
    public async Task HandleAsync(UpdateState message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.UpdateState(message.Id, message.Key, message.Items.Select(RegisterStateHandler.ToApplicationStateItem).ToList()), cancellationToken);
    }
}