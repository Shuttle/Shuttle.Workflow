namespace Shuttle.Workflow.SqlServer;

public interface IProcessDefinitionQuery
{
    ValueTask<bool> ContainsAsync(Models.ProcessDefinition.Specification specification, CancellationToken cancellationToken = default);
    Task<IEnumerable<Models.ProcessDefinition>> SearchAsync(Models.ProcessDefinition.Specification specification, CancellationToken cancellationToken = default);
}