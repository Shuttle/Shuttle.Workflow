using Shuttle.Hopper;
using Shuttle.Hopper.Scheduling.Messages.v1;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.Handlers;

public class ScheduleNotificationHandler : IContextMessageHandler<ScheduleNotification>
{
    public async Task HandleAsync(IHandlerContext<ScheduleNotification> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        switch (context.Message.Name.ToUpperInvariant())
        {
            case "WORKFLOW.REVIEWPROCESSES":
            {
                await context.SendAsync(new ReviewProcesses(), builder => builder.ToSelf(), cancellationToken);
                break;
            }
            case "WORKFLOW.REVIEWSEMAPHORES":
            {
                await context.SendAsync(new ReviewSemaphores(), builder => builder.ToSelf(), cancellationToken);
                break;
            }
        }
    }
}