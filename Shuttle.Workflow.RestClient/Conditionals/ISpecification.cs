namespace Shuttle.Workflow.RestClient;

public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate, DateTimeOffset? at = null);
}