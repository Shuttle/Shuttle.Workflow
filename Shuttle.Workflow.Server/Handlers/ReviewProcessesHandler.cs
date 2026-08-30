using Shuttle.Hopper;
using Shuttle.Recall;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;

namespace Shuttle.Workflow.Server.Handlers;

public class ReviewProcessesHandler(IProcessQuery processQuery, IEventStore eventStore) : IContextMessageHandler<ReviewProcesses>
{
    public async Task HandleAsync(IHandlerContext<ReviewProcesses> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var deferredProcesses = await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().IncludedStatus(Process.StatusNames.Deferred), cancellationToken);

        foreach (var deferredProcess in deferredProcesses)
        {
            await context.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{deferredProcess.Id}").ToSelf(), cancellationToken);
        }

        var activeProcesses = await processQuery.SearchAsync(new SqlServer.Models.Process.Specification().ActiveOnly().ExcludedStatus(Process.StatusNames.Failed), cancellationToken);

        foreach (var activeProcess in activeProcesses)
        {
            var stream = await eventStore.GetAsync(activeProcess.Id, cancellationToken: cancellationToken);
            var process = stream.Get<Process>();

            if (!process.IsOverdue())
            {
                continue;
            }

            // A waiting process has nothing in flight, so the overdue deadline is the end of it.  Any other status
            // means a message is still outstanding: abandoning would orphan whatever the target service is doing,
            // so raise it instead and leave the decision to the consumer.
            if (process.HasStatus(Process.StatusNames.Waiting))
            {
                stream.Add(process.Abandon($"Overdue at '{process.GetEffectiveOverdueAt():O}' while waiting."));

                await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);

                continue;
            }

            await context.SendAsync(new ProcessOverdue(), builder => builder.WithCorrelationId($"{activeProcess.Id}").ToSelf(), cancellationToken);
        }
    }
}