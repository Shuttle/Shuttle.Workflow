using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class DeferProcessHandler(IMediator mediator) : IMessageHandler<DeferProcess>
{
    public async Task HandleAsync(DeferProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.DeferProcess(message.ProcessId, message.DeferredTill), cancellationToken);
    }
}