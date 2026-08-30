namespace Shuttle.Workflow;

public interface ISemaphoreRepository
{
    Task<Semaphore?> FindAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    ValueTask<Guid?> SaveAsync(Semaphore semaphore, CancellationToken cancellationToken = default);
}