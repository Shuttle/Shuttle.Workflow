using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer;

public class ProcessDefinitionQuery(WorkflowDbContext dbContext) : IProcessDefinitionQuery
{
    public async Task<IEnumerable<Models.ProcessDefinition>> SearchAsync(Models.ProcessDefinition.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await GetQueryable(specification).OrderBy(item => item.Name).ToListAsync(cancellationToken);
    }

    public ValueTask<bool> ContainsAsync(Models.ProcessDefinition.Specification specification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return new(GetQueryable(specification).FirstOrDefault() != null);
    }

    private IQueryable<Models.ProcessDefinition> GetQueryable(Models.ProcessDefinition.Specification specification)
    {
        var queryable = dbContext.ProcessDefinitions
            .Include(item => item.Messages)
            .Include(item => item.StateItems)
            .AsQueryable();

        if (specification.Id.HasValue)
        {
            queryable = queryable.Where(item => item.Id == specification.Id.Value);
        }

        if (specification.Name != null)
        {
            queryable = queryable.Where(item => item.Name == specification.Name);
        }

        return queryable;
    }
}