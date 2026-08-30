using Shuttle.Hopper;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;

namespace Shuttle.Workflow.Server.Handlers;

public class ReviewSemaphoresHandler(WorkflowDbContext dbContext, ISemaphoreOptionsResolver semaphoreOptionsResolver, ISemaphoreRepository semaphoreRepository, ISemaphoreQuery semaphoreQuery) : IContextMessageHandler<ReviewSemaphores>
{
    public async Task HandleAsync(IHandlerContext<ReviewSemaphores> context, CancellationToken cancellationToken = default)
    {
        var semaphores = await semaphoreQuery.SearchAsync(new SqlServer.Models.Semaphore.Specification()
            .WithToExpiresAtExclusive(DateTimeOffset.UtcNow), cancellationToken);

        foreach (var semaphore in semaphores)
        {
            var options = semaphoreOptionsResolver.Resolve(semaphore.Key);

            if (options?.RemoveWhenExpired ?? false)
            {
                await semaphoreRepository.RemoveAsync(semaphore.Id, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}