using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer;

public class SemaphoreRepository(WorkflowDbContext dbContext) : ISemaphoreRepository
{
    public async ValueTask<Guid?> SaveAsync(Semaphore semaphore, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);

        var model = await dbContext.Semaphores.FirstOrDefaultAsync(item => item.Key == semaphore.Key, cancellationToken);

        if (model == null)
        {
            var id = Guid.NewGuid();

            model = new()
            {
                Id = id,
                Key = semaphore.Key
            };

            dbContext.Semaphores.Add(model);
        }

        model.Owner = semaphore.Owner;
        model.DateRegistered = semaphore.DateRegistered;
        model.ExpiresAt = semaphore.ExpiresAt;

        return model.Id;
    }

    public async Task<Semaphore?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Semaphores.FirstOrDefaultAsync(item => item.Key == key, cancellationToken);

        return model == null
            ? null
            : new Semaphore(model.Key, model.Owner, model.DateRegistered, model.ExpiresAt);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await dbContext.Semaphores.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (model == null)
        {
            return;
        }

        dbContext.Semaphores.Remove(model);
    }
}