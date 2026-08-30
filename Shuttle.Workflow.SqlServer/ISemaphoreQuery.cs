namespace Shuttle.Workflow.SqlServer;

public interface ISemaphoreQuery
{
    Task<IEnumerable<Models.Semaphore>> SearchAsync(Models.Semaphore.Specification specification, CancellationToken cancellationToken = default);
}