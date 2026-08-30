using Shuttle.Workflow.Conditionals;

namespace Shuttle.Workflow.RestClient;

public class StateConditionalCollectionService(IStateConditionalCollectionFactory stateConditionalCollectionFactory)
    : IStateConditionalCollectionService
{
    private readonly IStateConditionalCollectionFactory _stateConditionalCollectionFactory = stateConditionalCollectionFactory ?? throw new ArgumentNullException(nameof(stateConditionalCollectionFactory));
    private readonly Dictionary<string, StateConditionalCollection> _stateConditionals = new();

    public StateConditionalCollection Get(string conditionalName)
    {
        if (!_stateConditionals.TryGetValue(conditionalName, out var stateConditionalCollection))
        {
            throw new ArgumentException($"State conditional with name '{conditionalName}' not found.", nameof(conditionalName));
        }

        return stateConditionalCollection;
    }

    public void Add(string name, IEnumerable<StateConditionalOptions> stateConditionalOptions)
    {
        if (_stateConditionals.ContainsKey(name))
        {
            throw new ArgumentException($"There is already a state conditional named '{name}'.");
        }

        _stateConditionals.Add(name, _stateConditionalCollectionFactory.Create(stateConditionalOptions));
    }
}