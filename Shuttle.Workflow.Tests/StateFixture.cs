using NUnit.Framework;
using Shuttle.Workflow.Events.State.v1;

namespace Shuttle.Workflow.Tests;

[TestFixture]
public class StateFixture
{
    private static State Registered(Guid id)
    {
        var state = new State();

        state.Register($"[state]:{id}");

        return state;
    }

    [Test]
    public void Should_be_able_to_create_a_valid_state()
    {
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        var stringStateItem = new State.Item("string-name", "string-value", StateItemType.String, now);
        var dateTimeStateItem = new State.Item("datetime-name", now, StateItemType.DateTime, now);
        var decimalStateItem = new State.Item("decimal-name", 2.35, StateItemType.Decimal, now);
        var booleanStateItem = new State.Item("boolean-name", true, StateItemType.Boolean, now);
        var guidStateItem = new State.Item("guid-name", guid, StateItemType.Guid, now);

        Assert.That(state.AddItem(stringStateItem), Is.InstanceOf<ItemAdded>());
        Assert.That(state.AddItem(dateTimeStateItem), Is.InstanceOf<ItemAdded>());
        Assert.That(state.AddItem(decimalStateItem), Is.InstanceOf<ItemAdded>());
        Assert.That(state.AddItem(booleanStateItem), Is.InstanceOf<ItemAdded>());
        Assert.That(state.AddItem(guidStateItem), Is.InstanceOf<ItemAdded>());

        Assert.That(state.GetItems().Count, Is.EqualTo(5));

        Assert.That(state.GetItem(stringStateItem.Name).GetValue<string>(), Is.EqualTo(stringStateItem.Value));
        Assert.That(state.GetItem(dateTimeStateItem.Name).GetValue<DateTimeOffset>(), Is.EqualTo(dateTimeStateItem.Value));
        Assert.That(state.GetItem(dateTimeStateItem.Name).GetValue<DateTimeOffset>().Offset, Is.EqualTo(TimeSpan.Zero));

        // 'StateItemType.DateTime' values stay readable as a 'DateTime' so consuming services do not have to
        // move in step with this service.
        Assert.That(state.GetItem(dateTimeStateItem.Name).GetValue<DateTime>(), Is.EqualTo(now));
        Assert.That(state.GetItem(dateTimeStateItem.Name).GetValue<DateTime>().Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(state.GetItem(decimalStateItem.Name).GetValue<decimal>(), Is.EqualTo(decimalStateItem.Value));
        Assert.That(state.GetItem(booleanStateItem.Name).GetValue<bool>(), Is.EqualTo(booleanStateItem.Value));
        Assert.That(state.GetItem(guidStateItem.Name).GetValue<Guid>(), Is.EqualTo(guidStateItem.Value));

        Assert.That(() => state.AddItem(new(stringStateItem.Name, "new-string-value", StateItemType.Decimal, DateTime.UtcNow)), Throws.ArgumentException);
        Assert.That(() => state.AddItem(new(stringStateItem.Name, "new-string-value", StateItemType.String, DateTime.UtcNow)), Throws.Nothing);

        Assert.That(state.GetItem(stringStateItem.Name).GetValue<string>(), Is.EqualTo("new-string-value"));
    }

    [Test]
    public void Should_not_be_able_to_create_or_set_invalid_values()
    {
        var state = Registered(Guid.NewGuid());

        Assert.That(() => state.AddItem(new("guid", Guid.NewGuid(), StateItemType.Guid, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("guid", null, StateItemType.Guid, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("guid", "80D3B011-CF57-4E13-8240-B8BEAF55624A", StateItemType.Guid, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("guid", "not-a-guid", StateItemType.Guid, DateTime.UtcNow)), Throws.ArgumentException);

        Assert.That(() => state.AddItem(new("decimal", 1.5, StateItemType.Decimal, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("decimal", null, StateItemType.Decimal, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("decimal", "123.456", StateItemType.Decimal, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("decimal", "not-a-decimal", StateItemType.Decimal, DateTime.UtcNow)), Throws.ArgumentException);

        Assert.That(() => state.AddItem(new("date/time", DateTime.Now, StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", null, StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", "2024-08-15", StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", "2024/08/15", StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", "2024/08/15 5:05", StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", "2024-08-15T05:05", StateItemType.DateTime, DateTime.UtcNow)), Throws.Nothing);
        Assert.That(() => state.AddItem(new("date/time", "not-a-date", StateItemType.DateTime, DateTime.UtcNow)), Throws.ArgumentException);
    }

    [Test]
    public void Should_not_raise_an_event_when_the_value_has_not_changed()
    {
        var now = DateTime.UtcNow.Date;
        var guid = Guid.NewGuid();

        var state = Registered(guid);
        var added = 0;

        void AddItem(State.Item item)
        {
            if (state.AddItem(item) != null)
            {
                added++;
            }
        }

        AddItem(new("string-name", "string-value", StateItemType.String, now));

        Assert.That(added, Is.EqualTo(1));

        AddItem(new("string-name", "string-value", StateItemType.String, now));

        Assert.That(added, Is.EqualTo(1));

        AddItem(new("string-name", "string-value", StateItemType.String, DateTime.UtcNow.Date));

        Assert.That(added, Is.EqualTo(1));

        AddItem(new("string-name", "string-value-changed", StateItemType.String, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(2));

        AddItem(new("datetime-name", now, StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(3));

        AddItem(new("datetime-name", now.ToString("yyyy-MM-dd"), StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(3));

        AddItem(new("datetime-name", now.ToString("yyyy/MM/dd"), StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(3));

        AddItem(new("datetime-name", now.ToString("O"), StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(3));

        AddItem(new("datetime-name", now, StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(3));

        AddItem(new("datetime-name", now.AddDays(1), StateItemType.DateTime, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(4));

        AddItem(new("decimal-name", 2.35, StateItemType.Decimal, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(5));

        AddItem(new("decimal-name", 2.35, StateItemType.Decimal, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(5));

        AddItem(new("decimal-name", 2.351, StateItemType.Decimal, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(6));

        AddItem(new("boolean-name", true, StateItemType.Boolean, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(7));

        AddItem(new("boolean-name", true, StateItemType.Boolean, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(7));

        AddItem(new("boolean-name", false, StateItemType.Boolean, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(8));

        AddItem(new("guid-name", guid, StateItemType.Guid, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(9));

        AddItem(new("guid-name", guid, StateItemType.Guid, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(9));

        AddItem(new("guid-name", Guid.NewGuid(), StateItemType.Guid, DateTime.UtcNow));

        Assert.That(added, Is.EqualTo(10));
    }

    [Test]
    public void Should_be_able_to_get_relevant_state_items_as_at_given_date()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-3);
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        Assert.That(state.GetItems().Count, Is.Zero);

        state.AddItem(new("string-key", "string-value", StateItemType.String, date));

        Assert.That(state.GetItems().Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        state.AddItem(new("string-key", "string-value", StateItemType.String, date.AddDays(1)));

        Assert.That(state.GetItems().Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        state.AddItem(new("string-key", "string-value-changed", StateItemType.String, date.AddDays(1)));

        Assert.That(state.GetItems().Count, Is.EqualTo(2));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        state.AddItem(new("string-key", "string-value", StateItemType.String, date.AddDays(2)));

        Assert.That(state.GetItems().Count, Is.EqualTo(3));
        Assert.That(state.GetItems(date).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(1)).Count, Is.EqualTo(1));
        Assert.That(state.GetItems(date.AddDays(-1)).Count, Is.Zero);

        Assert.That(state.FindItem("string-key", date.AddDays(-1)), Is.Null);
        Assert.That(state.GetItem("string-key", date).Value, Is.EqualTo("string-value"));
        Assert.That(state.GetItem("string-key", date.AddDays(1)).Value, Is.EqualTo("string-value-changed"));
        Assert.That(state.GetItem("string-key", date.AddDays(2)).Value, Is.EqualTo("string-value"));

        Assert.That(state.GetItem("string-key", date).EffectiveDateEnd, Is.EqualTo(date.AddDays(1)));
        Assert.That(state.GetItem("string-key", date.AddDays(1)).EffectiveDateEnd, Is.EqualTo(date.AddDays(2)));
        Assert.That(state.GetItem("string-key", date.AddDays(2)).IsOpenEnded, Is.True);
    }

    [Test]
    public void Should_splice_a_back_dated_item_into_the_existing_interval_chain()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-10);
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        state.AddItem(new("string-key", "first", StateItemType.String, date));
        state.AddItem(new("string-key", "third", StateItemType.String, date.AddDays(4)));

        // Back-dated between the two existing items -- the predecessor must close against it, and it must close
        // against its successor rather than running open to the sentinel.
        var itemAdded = state.AddItem(new("string-key", "second", StateItemType.String, date.AddDays(2)));

        Assert.That(itemAdded, Is.InstanceOf<ItemAdded>());
        Assert.That(((ItemAdded)itemAdded!).ClosedPredecessorEffectiveDate, Is.EqualTo(date));

        Assert.That(state.GetItems().Count, Is.EqualTo(3));

        Assert.That(state.GetItem("string-key", date).Value, Is.EqualTo("first"));
        Assert.That(state.GetItem("string-key", date).EffectiveDateEnd, Is.EqualTo(date.AddDays(2)));

        Assert.That(state.GetItem("string-key", date.AddDays(2)).Value, Is.EqualTo("second"));
        Assert.That(state.GetItem("string-key", date.AddDays(2)).EffectiveDateEnd, Is.EqualTo(date.AddDays(4)));

        Assert.That(state.GetItem("string-key", date.AddDays(3)).Value, Is.EqualTo("second"));

        Assert.That(state.GetItem("string-key", date.AddDays(4)).Value, Is.EqualTo("third"));
        Assert.That(state.GetItem("string-key", date.AddDays(4)).IsOpenEnded, Is.True);

        Assert.That(state.GetItems(date.AddDays(3)).Count, Is.EqualTo(1));
    }

    [Test]
    public void Should_be_able_to_expire_an_item_without_a_replacement()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-10);
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        state.AddItem(new("string-key", "value", StateItemType.String, date));

        Assert.That(state.ContainsItem("string-key", date.AddDays(5)), Is.True);

        var itemExpired = state.ExpireItem("string-key", date.AddDays(3));

        Assert.That(itemExpired, Is.Not.Null);

        Assert.That(state.ContainsItem("string-key", date), Is.True);
        Assert.That(state.ContainsItem("string-key", date.AddDays(2)), Is.True);

        // Expiry is exclusive, and there is no replacement -- the name simply stops resolving.
        Assert.That(state.ContainsItem("string-key", date.AddDays(3)), Is.False);
        Assert.That(state.ContainsItem("string-key", date.AddDays(5)), Is.False);
        Assert.That(state.FindItem("string-key", date.AddDays(3)), Is.Null);
        Assert.That(state.GetItems(date.AddDays(3)).Count, Is.Zero);

        // The row survives -- expiry closes the interval, it does not delete.
        Assert.That(state.GetItems().Count, Is.EqualTo(1));

        Assert.That(() => state.ExpireItem("no-such-key", date), Throws.InvalidOperationException);
    }

    [Test]
    public void Should_not_re_open_an_expired_item_when_a_later_item_is_added()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-10);
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        state.AddItem(new("string-key", "value", StateItemType.String, date));
        state.ExpireItem("string-key", date.AddDays(2));

        state.AddItem(new("string-key", "later-value", StateItemType.String, date.AddDays(5)));

        // The splice must not stretch the expired interval from day 2 out to day 5 -- the gap is deliberate.
        Assert.That(state.GetItem("string-key", date).EffectiveDateEnd, Is.EqualTo(date.AddDays(2)));
        Assert.That(state.FindItem("string-key", date.AddDays(3)), Is.Null);
        Assert.That(state.GetItem("string-key", date.AddDays(5)).Value, Is.EqualTo("later-value"));
    }

    [Test]
    public void Should_be_able_to_remove_all_items_with_a_given_name()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero).AddDays(-10);
        var guid = Guid.NewGuid();

        var state = Registered(guid);

        state.AddItem(new("string-key", "first", StateItemType.String, date));
        state.AddItem(new("string-key", "second", StateItemType.String, date.AddDays(1)));
        state.AddItem(new("other-key", "value", StateItemType.String, date));

        var removed = state.RemoveItem("string-key").ToList();

        Assert.That(removed.Count, Is.EqualTo(2));
        Assert.That(state.GetItems().Count(), Is.EqualTo(1));
        Assert.That(state.RemoveItem("string-key").Any(), Is.False);
    }

    [Test]
    public void Should_not_accept_an_effective_date_end_that_precedes_the_effective_date()
    {
        var date = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        Assert.That(() => new State.Item("string-key", "value", StateItemType.String, date, date.AddDays(-1)), Throws.ArgumentException);
        Assert.That(() => new State.Item("string-key", "value", StateItemType.String, date, date), Throws.Nothing);
    }
}