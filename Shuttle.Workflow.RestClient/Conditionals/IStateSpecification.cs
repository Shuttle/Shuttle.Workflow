using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient;

public interface IStateSpecification : ISpecification<State>
{
    public string Source { get; }
}