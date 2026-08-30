using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[Index(nameof(Key), IsUnique = true, Name = $"IX_{nameof(State)}")]
public class State
{
    [Key]
    public Guid Id { get; set; }

    public ICollection<StateItem> Items { get; set; } = [];

    [StringLength(512)]
    [Required]
    public string Key { get; set; } = null!;

    public class Specification : Specification<Specification>
    {
        private readonly List<ItemMatch> _itemMatches = [];
        private readonly List<string> _keys = [];
        public bool HasKeys => _keys.Count > 0;
        public DateTimeOffset? ItemEffectiveDate { get; private set; }
        public DateTimeOffset? ItemEndEffectiveDateExclusive { get; private set; }

        public IEnumerable<ItemMatch> ItemMatches => _itemMatches.AsReadOnly();
        public DateTimeOffset? ItemStartEffectiveDate { get; private set; }

        public string? Key { get; private set; }
        public string? KeyMatch { get; private set; }
        public IEnumerable<string> Keys => _keys.AsReadOnly();

        public bool ShouldIncludeItems { get; private set; }

        public Specification AddKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (!_keys.Contains(key))
            {
                _keys.Add(key);
            }

            return this;
        }

        public Specification AddKeys(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                AddKey(key);
            }

            return this;
        }

        public Specification IncludeItems()
        {
            ShouldIncludeItems = true;
            return this;
        }

        public Specification WithItemEffectiveDate(DateTimeOffset date)
        {
            ItemEffectiveDate = date;

            return this;
        }

        public Specification WithItemEndEffectiveDateExclusive(DateTimeOffset date)
        {
            if (ItemStartEffectiveDate.HasValue && date <= ItemStartEffectiveDate.Value)
            {
                throw new ArgumentException($"Exclusive end effective date '{date:O}' is not after the item start effective date '{ItemStartEffectiveDate.Value:O}'.", nameof(date));
            }

            ItemEndEffectiveDateExclusive = date;
            return this;
        }

        public Specification WithItemMatch(ItemMatch itemMatch)
        {
            if (!ItemEffectiveDate.HasValue)
            {
                throw new InvalidOperationException("ItemEffectiveDate must be set before adding an item match.");
            }

            ArgumentNullException.ThrowIfNull(itemMatch);

            _itemMatches.Add(itemMatch);

            return this;
        }

        public Specification WithItemStartEffectiveDate(DateTimeOffset date)
        {
            if (ItemEndEffectiveDateExclusive.HasValue && date >= ItemEndEffectiveDateExclusive.Value)
            {
                throw new ArgumentException($"Item start effective date '{date:O}' is not before the exclusive end effective date '{ItemEndEffectiveDateExclusive.Value:O}'.", nameof(date));
            }

            ItemStartEffectiveDate = date;
            return this;
        }

        public Specification WithKey(string key)
        {
            Key = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentNullException(nameof(key));
            return this;
        }

        public Specification WithKeyMatch(string match)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(match);

            KeyMatch = match;
            return this;
        }

        public class ItemMatch(string name, string @operator, string? value, string type)
        {
            public string Name { get; } = name;
            public string Operator { get; } = @operator;
            public string Type { get; } = type;
            public string? Value { get; } = value;
        }
    }
}