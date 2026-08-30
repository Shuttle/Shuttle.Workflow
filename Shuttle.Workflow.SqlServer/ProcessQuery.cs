using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer;

public class ProcessQuery(WorkflowDbContext dbContext) : IProcessQuery
{
    public async Task<IEnumerable<Models.Process>> SearchAsync(Models.Process.Specification specification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await GetQueryable(dbContext, specification).ToListAsync(cancellationToken);
    }

    public async ValueTask<bool> ContainsAsync(Models.Process.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await new ValueTask<bool>(GetQueryable(dbContext, specification).FirstOrDefault() != null);
    }

    private IQueryable<Models.Process> GetQueryable(WorkflowDbContext dbContext, Models.Process.Specification specification)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(specification);

        var queryable = dbContext.Processes.AsNoTracking();

        if (specification.ShouldIncludeMessages)
        {
            queryable = queryable.Include(item => item.Messages.OrderBy(m => m.SequenceNumber));
        }

        if (specification.ShouldIncludeCommits)
        {
            queryable = queryable.Include(item => item.Commits.OrderBy(m => m.DateCommitted));
        }

        if (specification.HasIds)
        {
            queryable = queryable.Where(item => specification.Ids.Contains(item.Id));
        }

        if (specification.HasExcludedIds)
        {
            queryable = queryable.Where(item => !specification.ExcludedIds.Contains(item.Id));
        }

        if (specification.Name != null)
        {
            queryable = queryable.Where(item => item.Name == specification.Name);
        }

        if (specification.NameMatch != null)
        {
            queryable = queryable.Where(item => item.Name.Contains(specification.NameMatch));
        }

        if (specification.Key != null)
        {
            queryable = queryable.Where(item => item.Key == specification.Key);
        }

        if (!string.IsNullOrWhiteSpace(specification.KeyMatch))
        {
            queryable = queryable.Where(item => item.Key != null && item.Key.Contains(specification.KeyMatch));
        }

        if (specification.HasIncludedStatuses)
        {
            queryable = queryable.Where(item => specification.IncludedStatuses.Contains(item.Status));
        }

        if (specification.HasExcludedStatuses)
        {
            queryable = queryable.Where(item => !specification.ExcludedStatuses.Contains(item.Status));
        }

        if (specification.FromDateRegisteredInclusive.HasValue)
        {
            queryable = queryable.Where(item => item.DateRegistered >= specification.FromDateRegisteredInclusive);
        }

        if (specification.ToDateRegisteredExclusive.HasValue)
        {
            queryable = queryable.Where(item => item.DateRegistered < specification.ToDateRegisteredExclusive);
        }

        if (specification.FromDateCompletedInclusive.HasValue)
        {
            queryable = queryable.Where(item => item.DateCompleted >= specification.FromDateCompletedInclusive);
        }

        if (specification.ToDateCompletedExclusive.HasValue)
        {
            queryable = queryable.Where(item => item.DateCompleted < specification.ToDateCompletedExclusive);
        }

        if (specification.MaximumRows > 0)
        {
            queryable = queryable.OrderByDescending(item => item.DateRegistered).Take(specification.MaximumRows);
        }

        return queryable;
    }
}