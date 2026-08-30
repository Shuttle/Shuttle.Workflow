using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class AddProcessMessageHandler(IMediator mediator) : IMessageHandler<AddProcessMessage>
{
    public async Task HandleAsync(AddProcessMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.AddProcessMessage(message.ProcessId, message.TypeName), cancellationToken);
    }
}