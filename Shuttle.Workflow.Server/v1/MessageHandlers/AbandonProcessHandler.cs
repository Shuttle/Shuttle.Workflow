using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class AbandonProcessHandler(IMediator mediator) : IMessageHandler<AbandonProcess>
{
    public async Task HandleAsync(AbandonProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.AbandonProcess(message.ProcessId, message.Message), cancellationToken);
    }
}