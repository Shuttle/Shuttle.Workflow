namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class State
{
    public Guid? Id { get; set; }
    public List<Item> Items { get; set; } = [];
    public string Key { get; set; } = string.Empty;

    public class Expiry
    {
        /// <summary>
        ///     Exclusive instant from which the item no longer applies.  When omitted the item is expired as at
        ///     the time the request is handled.
        /// </summary>
        public DateTimeOffset? EffectiveDateEnd { get; set; }

        public string Name { get; set; } = null!;
    }

    public class Item
    {
        public DateTimeOffset DateRegistered { get; set; }
        public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UnixEpoch;

        /// <summary>
        ///     Exclusive end of the interval over which this item applies; <see cref="DateTimeOffset.MaxValue" />
        ///     means "still in effect".  Format it through <see cref="DateTimeOffset.UtcDateTime" /> --
        ///     <c>ToOffset</c> and <c>ToLocalTime</c> throw on <see cref="DateTimeOffset.MaxValue" /> for any
        ///     positive offset.
        /// </summary>
        public DateTimeOffset EffectiveDateEnd { get; set; } = DateTimeOffset.MaxValue;

        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Value { get; set; }
    }

    public class Specification
    {
        public Guid? Id { get; set; }
        public DateTimeOffset? ItemEffectiveDate { get; set; }
        public DateTimeOffset? ItemEndEffectiveDateExclusive { get; set; }
        public List<ItemMatch> ItemMatches { get; set; } = [];
        public DateTimeOffset? ItemStartEffectiveDate { get; set; }

        public string? Key { get; set; }
        public string? KeyMatch { get; set; }
        public int MaximumRows { get; set; }

        public class ItemMatch
        {
            public string Name { get; set; } = string.Empty;
            public string Operator { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Value { get; set; }
        }
    }
}