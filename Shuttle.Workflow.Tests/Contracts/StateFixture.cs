using NUnit.Framework;
using Shuttle.Workflow.RestClient.v1;

namespace Shuttle.Workflow.Tests.Contracts;

[TestFixture]
public class StateFixture
{
    [Test]
    public void Should_be_able_to_get_a_state_item_with_unix_epoch_effective_date()
    {
        var item = new WebApi.Contracts.v1.State.Item
        {
            Name = "string-key",
            Value = "string-value",
            Type = "String"
        };

        Assert.That(item.EffectiveDate, Is.EqualTo(DateTimeOffset.UnixEpoch));
        Assert.That(item.EffectiveDateEnd, Is.EqualTo(DateTimeOffset.MaxValue));
    }

    [Test]
    public void Should_be_able_to_get_relevant_state_items_as_at_given_date()
    {
        // Explicitly a 'DateTimeOffset' at a zero offset: 'DateTimeOffset.UtcNow.Date' hands back an
        // *unspecified*-kind 'DateTime', which the implicit conversion would then stamp with the local offset.
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-3);
        var guid = Guid.NewGuid();

        var state = new WebApi.Contracts.v1.State
        {
            Id = guid,
            Key = $"[state]:{guid}"
        };


        Assert.That(state.Items.Count, Is.Zero);

        state.AddItem("string-key", "string-value", "String", date);

        Assert.That(state.Items.Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        state.AddItem("string-key", "string-value-changed", "String", date.AddDays(1));

        Assert.That(state.Items.Count, Is.EqualTo(2));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        state.AddItem("string-key", "string-value", "String", date.AddDays(2));

        Assert.That(state.Items.Count, Is.EqualTo(3));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        Assert.That(state.FindItem("string-key", date.AddDays(-1)), Is.Null);
        Assert.That(state.GetItem("string-key", date).Value, Is.EqualTo("string-value"));
        Assert.That(state.GetItem("string-key", date.AddDays(1)).Value, Is.EqualTo("string-value-changed"));
        Assert.That(state.GetItem("string-key", date.AddDays(2)).Value, Is.EqualTo("string-value"));

        // The splice closes each item off against its successor rather than leaving every interval open.
        Assert.That(state.GetItem("string-key", date).EffectiveDateEnd, Is.EqualTo(date.AddDays(1)));
        Assert.That(state.GetItem("string-key", date.AddDays(1)).EffectiveDateEnd, Is.EqualTo(date.AddDays(2)));
        Assert.That(state.GetItem("string-key", date.AddDays(2)).EffectiveDateEnd, Is.EqualTo(DateTimeOffset.MaxValue));
    }

    [Test]
    public void Should_be_able_to_expire_a_contract_state_item_without_a_replacement()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-3);
        var guid = Guid.NewGuid();

        var state = new WebApi.Contracts.v1.State
        {
            Id = guid,
            Key = $"[state]:{guid}"
        };

        state.AddItem("string-key", "string-value", "String", date);

        state.GetItem("string-key", date).EffectiveDateEnd = date.AddDays(1);

        Assert.That(state.FindItem("string-key", date), Is.Not.Null);
        Assert.That(state.FindItem("string-key", date.AddDays(1)), Is.Null);
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.Zero);
    }
}