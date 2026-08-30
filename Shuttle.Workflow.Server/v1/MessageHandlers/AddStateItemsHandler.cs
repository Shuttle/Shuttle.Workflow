using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class AddStateItemsHandler(IMediator mediator) : IMessageHandler<AddStateItems>
{
    public async Task HandleAsync(AddStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.AddStateItems(message.StateId, message.Items.Select(RegisterStateHandler.ToApplicationStateItem).ToList()), cancellationToken);
    }
}