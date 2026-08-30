using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class RemoveStateItemsHandler(IMediator mediator) : IMessageHandler<RemoveStateItems>
{
    public async Task HandleAsync(RemoveStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.RemoveStateItems(message.StateId, message.Names), cancellationToken);
    }
}