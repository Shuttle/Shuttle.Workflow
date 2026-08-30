using NUnit.Framework;
using Shuttle.Workflow.Conditionals;
using Shuttle.Workflow.RestClient;

namespace Shuttle.Workflow.Tests.Contracts;

[TestFixture]
public class StateConditionalCollectionFactoryFixture
{
    [Test]
    public void Should_be_able_to_create_a_state_conditional_collection_from_the_given_options()
    {
        var factory = new StateConditionalCollectionFactory(new StateSpecificationFactory());

        var userGroups = new List<StateConditionalOptions>
        {
            new()
            {
                Value = "the-group",
                Specifications =
                [
                    new()
                    {
                        Type = "Regex",
                        Source = "the-source",
                        Value = "the-value"
                    }
                ]
            }
        };

        var stateConditionalCollection = factory.Create(userGroups);

        var state = new WebApi.Contracts.v1.State();

        state.Items.Add(new() { Name = "the-source", Type = "String", Value = "Has the-value here." });

        Assert.That(stateConditionalCollection, Is.Not.Null);
        Assert.That(stateConditionalCollection.GetValues(state).Count(), Is.EqualTo(1));
    }
}