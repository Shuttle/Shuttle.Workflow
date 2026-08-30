using Microsoft.Extensions.Logging;
using Shuttle.Hopper;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.Handlers;

public class ProcessOverdueHandler(ILogger<ProcessOverdueHandler> logger) : IContextMessageHandler<ProcessOverdue>
{
    public Task HandleAsync(IHandlerContext<ProcessOverdue> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogWarning("[overdue] : ProcessId = '{ProcessId}'.", context.GetProcessId());

        return Task.CompletedTask;
    }
}