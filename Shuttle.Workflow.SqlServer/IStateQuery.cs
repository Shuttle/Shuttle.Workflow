namespace Shuttle.Workflow.SqlServer;

public interface IStateQuery
{
    Task<IEnumerable<Models.State>> SearchAsync(Models.State.Specification specification, CancellationToken cancellationToken = default);
}