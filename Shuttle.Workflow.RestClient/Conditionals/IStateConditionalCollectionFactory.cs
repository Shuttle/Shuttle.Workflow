using Shuttle.Workflow.Conditionals;

namespace Shuttle.Workflow.RestClient;

public interface IStateConditionalCollectionFactory
{
    StateConditionalCollection Create(IEnumerable<StateConditionalOptions> stateConditionalOptions);
}