using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class CompleteProcessMessageHandler(IMediator mediator) : IMessageHandler<CompleteProcessMessage>
{
    public async Task HandleAsync(CompleteProcessMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.CompleteProcessMessage(message.ProcessId, message.MessageId, message.DeferredTill), cancellationToken);
    }
}