using NUnit.Framework;
using Shuttle.Workflow.RestClient;

namespace Shuttle.Workflow.Tests.Contracts;

[TestFixture]
public class StateConditionalCollectionFixture
{
    [Test]
    public void Should_be_able_to_get_group_names_matching_the_given_state()
    {
        var collection = new StateConditionalCollection();

        var state = new WebApi.Contracts.v1.State();

        state.Items.Add(new()
        {
            Name = "key",
            Type = "String",
            Value = "value"
        });

        Assert.That(collection.GetValues(state).Count(), Is.Zero);

        var groupA = new StateConditional("group-a");

        collection.Add(groupA);

        Assert.That(collection.GetValues(state).Count(), Is.EqualTo(1));
        Assert.That(collection.GetValues(state).First(), Is.EqualTo("group-a"));

        groupA.AddSpecification(new RegexStateStateSpecification("key", "no-match"));

        Assert.That(collection.GetValues(state).Count(), Is.EqualTo(0));

        var groupB = new StateConditional("group-b");

        collection.Add(groupB);

        groupB.AddSpecification(new RegexStateStateSpecification("key", "val.*"));

        Assert.That(collection.GetValues(state).Count(), Is.EqualTo(1));
        Assert.That(collection.GetValues(state).First(), Is.EqualTo("group-b"));
    }
}