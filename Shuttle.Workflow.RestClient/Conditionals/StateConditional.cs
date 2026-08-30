using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient;

public class StateConditional(string value) : ISpecification<State>
{
    private readonly List<IStateSpecification> _specifications = [];
    public string Value { get; } = !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentNullException(nameof(value));

    public bool IsSatisfiedBy(State candidate, DateTimeOffset? at = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return _specifications.All(specification => specification.IsSatisfiedBy(candidate, at));
    }

    public void AddSpecification(IStateSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specifications.Add(specification);
    }

    public IEnumerable<string> GetSources()
    {
        return _specifications.Select(specification => specification.Source).Distinct();
    }
}