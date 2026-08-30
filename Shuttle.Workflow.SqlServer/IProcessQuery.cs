namespace Shuttle.Workflow.SqlServer;

public interface IProcessQuery
{
    ValueTask<bool> ContainsAsync(Models.Process.Specification specification, CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.Process>> SearchAsync(Models.Process.Specification specification, CancellationToken cancellationToken = default);
}