using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class SetProcessMessageProgressHandler(IMediator mediator) : IMessageHandler<SetProcessMessageProgress>
{
    public async Task HandleAsync(SetProcessMessageProgress message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.SetProcessMessageProgress(message.ProcessId, message.MessageId, message.ItemsTotal, message.ItemsCompleted), cancellationToken);
    }
}
