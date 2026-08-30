using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class CommitProcessHandler(IMediator mediator) : IMessageHandler<CommitProcess>
{
    public async Task HandleAsync(CommitProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.CommitProcess(message.ProcessId, message.Key, message.DateCommitted), cancellationToken);
    }
}