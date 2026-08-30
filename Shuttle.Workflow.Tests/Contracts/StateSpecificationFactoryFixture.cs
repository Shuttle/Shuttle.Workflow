using NUnit.Framework;
using Shuttle.Workflow.RestClient;

namespace Shuttle.Workflow.Tests.Contracts;

[TestFixture]
public class StateSpecificationFactoryFixture
{
    [Test]
    public void Should_be_able_to_create_all_relevant_types()
    {
        var factory = new StateSpecificationFactory();

        Assert.That(() => factory.Create("bogus", "source", "value"), Throws.TypeOf<NotSupportedException>());

        var state = new WebApi.Contracts.v1.State();

        state.Items.Add(new()
        {
            Name = "key",
            Type = "String",
            Value = "some value"
        });

        var regex = factory.Create("Regex", "key", ".+val.*");

        Assert.That(regex.IsSatisfiedBy(state), Is.True);

        var equals = factory.Create("Equals", "key", "some value");

        Assert.That(equals.IsSatisfiedBy(state), Is.True);

        var startWith = factory.Create("StartsWith", "key", "some");

        Assert.That(startWith.IsSatisfiedBy(state), Is.True);

        var endsWith = factory.Create("EndsWith", "key", "value");

        Assert.That(endsWith.IsSatisfiedBy(state), Is.True);

        var contains = factory.Create("Contains", "key", "me");

        Assert.That(contains.IsSatisfiedBy(state), Is.True);
    }
}