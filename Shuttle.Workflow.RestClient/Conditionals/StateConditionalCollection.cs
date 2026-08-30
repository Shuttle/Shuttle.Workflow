using System.Collections;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient;

public class StateConditionalCollection : IEnumerable<StateConditional>
{
    private readonly List<StateConditional> _stateConditionals = [];

    public IEnumerator<StateConditional> GetEnumerator()
    {
        return _stateConditionals.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(StateConditional stateConditional)
    {
        ArgumentNullException.ThrowIfNull(stateConditional);

        _stateConditionals.Add(stateConditional);
    }

    public IEnumerable<string> GetSources()
    {
        var result = new List<string>();

        foreach (var stateConditional in _stateConditionals)
        {
            result.AddRange(stateConditional.GetSources());
        }

        return result.Distinct();
    }

    public IEnumerable<string> GetValues(State state, DateTimeOffset? at = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return _stateConditionals
            .Where(item => item.IsSatisfiedBy(state, atDateTimeOffset))
            .Select(item => item.Value)
            .Distinct();
    }
}