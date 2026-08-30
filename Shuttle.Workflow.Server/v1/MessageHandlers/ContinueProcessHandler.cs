using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class ContinueProcessHandler(IMediator mediator) : IMessageHandler<ContinueProcess>
{
    public async Task HandleAsync(ContinueProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.ContinueProcess(message.ProcessId), cancellationToken);
    }
}