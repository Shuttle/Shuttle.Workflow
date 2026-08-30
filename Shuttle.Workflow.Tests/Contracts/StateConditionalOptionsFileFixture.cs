using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shuttle.Workflow.Conditionals;
using Shuttle.Workflow.RestClient;

namespace Shuttle.Workflow.Tests.Contracts;

[TestFixture]
public class StateConditionalOptionsFileFixture
{
    [Test]
    public void Should_be_able_to_get_group_names_matching_the_given_state_in_an_options_file()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".files", "StateConditionalOptions.json");

        Assert.That(File.Exists(path), Is.True);

        var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();

        var stateConditionals = configuration.GetSection("StateConditionals").Get<StateConditionalOptions[]>();

        Assert.That(stateConditionals, Is.Not.Null);

        var collection = new StateConditionalCollectionFactory(new StateSpecificationFactory()).Create(stateConditionals!);

        var state = new WebApi.Contracts.v1.State();

        state.Items.Add(new()
        {
            Name = "BusinessUnit",
            Type = "String",
            Value = "Operations"
        });

        state.Items.Add(new()
        {
            Name = "Country",
            Type = "String",
            Value = "SA"
        });

        state.Items.Add(new()
        {
            Name = "City",
            Type = "String",
            Value = "Sydney"
        });

        var names = collection.GetValues(state).ToList();

        Assert.That(names.Count, Is.EqualTo(2));
        Assert.That(names, Contains.Item("groupa@domain.com"));
        Assert.That(names, Contains.Item("groupc@domain.com"));

        state = new();

        state.Items.Add(new()
        {
            Name = "BusinessUnit",
            Type = "String",
            Value = "Support"
        });

        state.Items.Add(new()
        {
            Name = "Country",
            Type = "String",
            Value = "SA"
        });

        state.Items.Add(new()
        {
            Name = "City",
            Type = "String",
            Value = "Sydney"
        });

        names = collection.GetValues(state).ToList();

        Assert.That(names.Count, Is.EqualTo(2));
        Assert.That(names, Contains.Item("groupb@domain.com"));
        Assert.That(names, Contains.Item("groupc@domain.com"));
    }
}