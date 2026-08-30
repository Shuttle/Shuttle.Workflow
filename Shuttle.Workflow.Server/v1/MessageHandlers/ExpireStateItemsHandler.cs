using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class ExpireStateItemsHandler(IMediator mediator) : IMessageHandler<ExpireStateItems>
{
    public async Task HandleAsync(ExpireStateItems message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.ExpireStateItems(message.StateId, message.Expiries.Select(item => new Application.ExpireStateItems.Expiry { Name = item.Name, EffectiveDateEnd = item.EffectiveDateEnd }).ToList()), cancellationToken);
    }
}