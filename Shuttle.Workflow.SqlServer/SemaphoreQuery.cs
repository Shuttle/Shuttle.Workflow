using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer;

public class SemaphoreQuery(WorkflowDbContext dbContext) : ISemaphoreQuery
{
    public async Task<IEnumerable<Models.Semaphore>> SearchAsync(Models.Semaphore.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var queryable = dbContext.Semaphores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(specification.Key))
        {
            queryable = queryable.Where(item => item.Key == specification.Key);
        }

        if (!string.IsNullOrWhiteSpace(specification.KeyMatch))
        {
            queryable = queryable.Where(item => item.Key.Contains(specification.KeyMatch));
        }

        if (!string.IsNullOrWhiteSpace(specification.Owner))
        {
            queryable = queryable.Where(item => item.Owner == specification.Owner);
        }

        if (!string.IsNullOrWhiteSpace(specification.OwnerMatch))
        {
            queryable = queryable.Where(item => item.Owner.Contains(specification.OwnerMatch));
        }

        if (specification.FromExpiresAtInclusive.HasValue)
        {
            queryable = queryable.Where(item => item.ExpiresAt >= specification.FromExpiresAtInclusive);
        }

        if (specification.ToExpiresAtExclusive.HasValue)
        {
            queryable = queryable.Where(item => item.ExpiresAt < specification.ToExpiresAtExclusive);
        }

        if (specification.MaximumRows > 0)
        {
            queryable = queryable.Take(specification.MaximumRows);
        }

        return await queryable.ToListAsync(cancellationToken);
    }
}