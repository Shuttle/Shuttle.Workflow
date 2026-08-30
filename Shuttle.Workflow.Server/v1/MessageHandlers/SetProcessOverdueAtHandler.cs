using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class SetProcessOverdueAtHandler(IMediator mediator) : IMessageHandler<SetProcessOverdueAt>
{
    public async Task HandleAsync(SetProcessOverdueAt message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.SetProcessOverdueAt(message.ProcessId, message.OverdueAt), cancellationToken);
    }
}