using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class FailProcessHandler(IMediator mediator) : IMessageHandler<FailProcess>
{
    public async Task HandleAsync(FailProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.FailProcess(message.ProcessId, message.Message), cancellationToken);
    }
}