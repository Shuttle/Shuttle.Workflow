using Shuttle.Workflow.Conditionals;

namespace Shuttle.Workflow.RestClient;

public class StateConditionalCollectionFactory(IStateSpecificationFactory specificationFactory) : IStateConditionalCollectionFactory
{
    private readonly IStateSpecificationFactory _specificationFactory = specificationFactory ?? throw new ArgumentNullException(nameof(specificationFactory));

    public StateConditionalCollection Create(IEnumerable<StateConditionalOptions> stateConditionalOptions)
    {
        ArgumentNullException.ThrowIfNull(stateConditionalOptions);

        var result = new StateConditionalCollection();

        foreach (var options in stateConditionalOptions)
        {
            var stateConditional = new StateConditional(options.Value);

            foreach (var stateSpecificationOptions in options.Specifications)
            {
                stateConditional.AddSpecification(_specificationFactory.Create(
                    stateSpecificationOptions.Type,
                    stateSpecificationOptions.Source,
                    stateSpecificationOptions.Value));
            }

            result.Add(stateConditional);
        }

        return result;
    }
}