using Shuttle.Workflow.Conditionals;

namespace Shuttle.Workflow.RestClient;

public interface IStateConditionalCollectionService
{
    void Add(string name, IEnumerable<StateConditionalOptions> stateConditionalOptions);
    StateConditionalCollection Get(string conditionalName);
}