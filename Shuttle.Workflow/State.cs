using System.Globalization;
using Shuttle.Workflow.Events.State.v1;

namespace Shuttle.Workflow;

public class State
{
    private readonly List<Item> _items = [];

    public string Key { get; private set; } = string.Empty;

    public object? AddItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var match = FindItem(item.Name, item.EffectiveDate);

        if (match is { Value: not null } &&
            item.Value != null &&
            match.Value.Equals(item.Value))
        {
            return null;
        }

        var existing = _items.FirstOrDefault(existing => existing.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase) && existing.EffectiveDate == item.EffectiveDate);

        if (existing != null && existing.Value != item.Value)
        {
            return On(new ItemValueChanged
            {
                Name = item.Name,
                EffectiveDate = item.EffectiveDate,
                Value = item.Value == null ? null : item.GetStringValue(),
                Type = item.Type
            });
        }

        // Splices the item into the effective-dated chain for its name: the item's end comes from its successor
        // and its predecessor's end from the item.  Back-dated inserts are legal, so the predecessor is not
        // necessarily the most recent item.  Ends only ever move earlier, so an item expired without a
        // replacement is never silently re-opened.
        var siblings = _items
            .Where(candidate => candidate.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var boundary = siblings
            .Where(candidate => candidate.EffectiveDate > item.EffectiveDate)
            .OrderBy(candidate => candidate.EffectiveDate)
            .FirstOrDefault()?.EffectiveDate ?? DateTimeOffset.MaxValue;

        var effectiveDateEnd = item.EffectiveDateEnd > boundary ? boundary : item.EffectiveDateEnd;

        var predecessor = siblings
            .Where(candidate => candidate.EffectiveDate < item.EffectiveDate)
            .OrderByDescending(candidate => candidate.EffectiveDate)
            .FirstOrDefault();

        // Deliberately not a separate event: closing the predecessor is a mechanical consequence of the append,
        // carried on the same 'ItemAdded' event so the projection updates both rows from one event.
        var closedPredecessorEffectiveDate = predecessor != null && predecessor.EffectiveDateEnd > item.EffectiveDate
            ? predecessor.EffectiveDate
            : (DateTimeOffset?)null;

        return On(new ItemAdded
        {
            Name = item.Name,
            Value = item.Value == null ? null : item.GetStringValue(),
            Type = item.Type,
            EffectiveDate = item.EffectiveDate,
            EffectiveDateEnd = effectiveDateEnd,
            ClosedPredecessorEffectiveDate = closedPredecessorEffectiveDate
        });
    }

    public bool ContainsItem(string name, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return _items
            .Any(item => item.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && item.EffectiveDate <= atDateTimeOffset && item.EffectiveDateEnd > atDateTimeOffset);
    }

    public ItemExpired? ExpireItem(string name, DateTimeOffset effectiveDateEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var item = FindItem(name, effectiveDateEnd) ?? throw new InvalidOperationException($"State item with name '{name}' at '{effectiveDateEnd:O}' not found.");

        return item.EffectiveDateEnd == effectiveDateEnd
            ? null
            : On(new ItemExpired { Name = item.Name, EffectiveDate = item.EffectiveDate, EffectiveDateEnd = effectiveDateEnd });
    }

    public Item? FindItem(string name, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return _items
            .OrderByDescending(item => item.EffectiveDate)
            .FirstOrDefault(item => item.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && item.EffectiveDate <= atDateTimeOffset && item.EffectiveDateEnd > atDateTimeOffset);
    }

    public Item GetItem(string name, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return FindItem(name, atDateTimeOffset) ?? throw new ArgumentException($"State item with name '{name}' at '{atDateTimeOffset:O}' not found.");
    }

    public IEnumerable<Item> GetItems()
    {
        return _items.AsReadOnly();
    }

    public IEnumerable<Item> GetItems(DateTimeOffset at)
    {
        return _items.Where(item => item.EffectiveDate <= at && item.EffectiveDateEnd > at);
    }

    private Registered On(Registered registered)
    {
        Key = registered.Key;

        return registered;
    }

    private KeySet On(KeySet keySet)
    {
        Key = keySet.Key;

        return keySet;
    }

    private ItemValueChanged On(ItemValueChanged itemValueChanged)
    {
        var item = _items.First(candidate => candidate.Name.Equals(itemValueChanged.Name, StringComparison.InvariantCultureIgnoreCase) && candidate.EffectiveDate == itemValueChanged.EffectiveDate);

        if (!item.SetValue(itemValueChanged.Value))
        {
            throw new ApplicationException($"Could not change value of item named '{itemValueChanged.Name}' with effective date '{itemValueChanged.EffectiveDate:O}'.");
        }

        return itemValueChanged;
    }

    private ItemAdded On(ItemAdded itemAdded)
    {
        if (itemAdded.ClosedPredecessorEffectiveDate.HasValue)
        {
            _items
                .First(candidate => candidate.Name.Equals(itemAdded.Name, StringComparison.InvariantCultureIgnoreCase) && candidate.EffectiveDate == itemAdded.ClosedPredecessorEffectiveDate.Value)
                .CloseAt(itemAdded.EffectiveDate);
        }

        _items.Add(new(itemAdded.Name, itemAdded.Value, itemAdded.Type, itemAdded.EffectiveDate, itemAdded.EffectiveDateEnd));

        return itemAdded;
    }

    private ItemExpired On(ItemExpired itemExpired)
    {
        _items
            .First(candidate => candidate.Name.Equals(itemExpired.Name, StringComparison.InvariantCultureIgnoreCase) && candidate.EffectiveDate == itemExpired.EffectiveDate)
            .CloseAt(itemExpired.EffectiveDateEnd);

        return itemExpired;
    }

    private ItemRemoved On(ItemRemoved itemRemoved)
    {
        _items.RemoveAll(item => item.Name.Equals(itemRemoved.Name, StringComparison.InvariantCultureIgnoreCase) && item.EffectiveDate == itemRemoved.EffectiveDate);

        return itemRemoved;
    }

    public Registered Register(string key)
    {
        return On(new Registered { Key = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentNullException(nameof(key)) });
    }

    public IEnumerable<ItemRemoved> RemoveItem(string name)
    {
        var itemsToRemove = _items
            .Where(item => item.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(item => item.EffectiveDate)
            .ToList();

        return itemsToRemove.Select(item => On(new ItemRemoved { Name = item.Name, EffectiveDate = item.EffectiveDate })).ToList();
    }

    public KeySet WithKey(string key)
    {
        return On(new KeySet { Key = key });
    }

    public class Item
    {
        public Item(string name, object? value, StateItemType type, DateTimeOffset effectiveDate, DateTimeOffset? effectiveDateEnd = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
            Type = type;
            EffectiveDate = effectiveDate;
            EffectiveDateEnd = effectiveDateEnd ?? DateTimeOffset.MaxValue;

            if (EffectiveDateEnd < EffectiveDate)
            {
                throw new ArgumentException($"Item with name '{name}' has an effective date end of '{EffectiveDateEnd:O}' which precedes its effective date of '{EffectiveDate:O}'.", nameof(effectiveDateEnd));
            }

            SetValue(value);
        }

        public DateTimeOffset EffectiveDate { get; }

        /// <summary>
        ///     Exclusive end of the interval over which this item applies.  <see cref="DateTimeOffset.MaxValue" />
        ///     means "still in effect"; an earlier value means the item expired, whether or not a replacement
        ///     followed it.  Note that <see cref="DateTimeOffset.ToOffset" /> and
        ///     <see cref="DateTimeOffset.ToLocalTime" /> throw on <see cref="DateTimeOffset.MaxValue" /> for any
        ///     positive offset -- read it through <see cref="DateTimeOffset.UtcDateTime" /> when formatting.
        /// </summary>
        public DateTimeOffset EffectiveDateEnd { get; private set; }

        public bool IsOpenEnded => EffectiveDateEnd == DateTimeOffset.MaxValue;

        public string Name { get; }
        public StateItemType Type { get; }
        public object? Value { get; private set; }

        internal bool CloseAt(DateTimeOffset effectiveDateEnd)
        {
            if (EffectiveDateEnd == effectiveDateEnd)
            {
                return false;
            }

            EffectiveDateEnd = effectiveDateEnd;

            return true;
        }

        public string GetStringValue()
        {
            return Type switch
            {
                StateItemType.Boolean => Value?.ToString() ?? string.Empty,
                // Formatted through 'UtcDateTime' so the stored string keeps the trailing 'Z' it has always
                // had -- emitting '+00:00' instead would make existing rows compare unequal to new ones.
                StateItemType.DateTime => ((DateTimeOffset?)Value)?.UtcDateTime.ToString("O") ?? string.Empty,
                StateItemType.Decimal => ((decimal?)Value)?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                StateItemType.Guid => ((Guid?)Value)?.ToString("D") ?? string.Empty,
                _ => Value?.ToString() ?? string.Empty
            };
        }

        // 'StateItemType.DateTime' values are held as a 'DateTimeOffset', but both a 'DateTime' and a
        // 'DateTimeOffset' may be asked for -- 'Convert.ChangeType' supports neither conversion.
        public T? GetValue<T>()
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType != typeof(DateTime) && underlyingType != typeof(DateTimeOffset))
            {
                return Value == null ? default : (T)Convert.ChangeType(Value, underlyingType);
            }

            if (Value == null)
            {
                return default;
            }

            var dateTimeOffset = Value switch
            {
                DateTimeOffset offsetValue => offsetValue,
                DateTime dateTimeValue => new(DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc)),
                _ => throw new InvalidOperationException($"Item with name '{Name}' has value '{Value}' which cannot be converted to a '{underlyingType.Name}' object.")
            };

            return underlyingType == typeof(DateTimeOffset)
                ? (T)(object)dateTimeOffset
                : (T)(object)dateTimeOffset.UtcDateTime;
        }

        internal bool SetValue(object? value)
        {
            if (Value == null && value == null)
            {
                return false;
            }

            if (value == null)
            {
                Value = null;

                return true;
            }

            switch (Type)
            {
                case StateItemType.DateTime:
                {
                    DateTimeOffset dateTimeOffsetValue;

                    switch (value)
                    {
                        case DateTimeOffset dateTimeOffset:
                        {
                            dateTimeOffsetValue = dateTimeOffset;

                            break;
                        }
                        // A bare 'DateTime' has always been taken to be UTC regardless of its 'Kind', and item
                        // values stay backwards compatible, so that is preserved rather than letting the
                        // implicit conversion apply the local offset.
                        case DateTime dateTime:
                        {
                            dateTimeOffsetValue = new(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));

                            break;
                        }
                        default:
                        {
                            // 'AssumeUniversal' is required: without it 'DateTimeOffset.TryParse' resolves a
                            // string carrying no offset designator to the *local* offset, where
                            // 'DateTime.TryParse' left it unspecified and unshifted.  Omitting it would
                            // silently move every date parsed from a bare string by the host's offset.
                            if (!DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTimeOffsetValueConverted))
                            {
                                throw new ArgumentException($"Item with name '{Name}' has an invalid date/time value of '{value}'.");
                            }

                            dateTimeOffsetValue = dateTimeOffsetValueConverted;

                            break;
                        }
                    }

                    if (Value != null && (DateTimeOffset)Value == dateTimeOffsetValue)
                    {
                        return false;
                    }

                    Value = dateTimeOffsetValue;

                    return true;
                }
                case StateItemType.Decimal:
                {
                    // Invariant culture is required: the host's regional settings (e.g. 'en-ZA') may use a
                    // comma decimal separator, which would otherwise reject an invariant-formatted value like
                    // '2.35' -- or worse, silently misparse it under a different digit-grouping convention.
                    if (!decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                    {
                        throw new ArgumentException($"Item with name '{Name}' has an invalid decimal value of '{value}'.");
                    }

                    if (Value != null && (decimal)Value == decimalValue)
                    {
                        return false;
                    }

                    Value = decimalValue;

                    return true;
                }
                case StateItemType.Boolean:
                {
                    if (!bool.TryParse(value.ToString(), out var booleanValue))
                    {
                        throw new ArgumentException($"Item with name '{Name}' has an invalid boolean value of '{value}'.");
                    }

                    if (Value != null && (bool)Value == booleanValue)
                    {
                        return false;
                    }

                    Value = booleanValue;

                    return true;
                }
                case StateItemType.Guid:
                {
                    if (!Guid.TryParse(value.ToString(), out var guidValue))
                    {
                        throw new ArgumentException($"Item with name '{Name}' has an invalid GUID value of '{value}'.");
                    }

                    if (Value != null && (Guid)Value == guidValue)
                    {
                        return false;
                    }

                    Value = guidValue;

                    return true;
                }
                default:
                {
                    if (Value != null && Value.Equals(value))
                    {
                        return false;
                    }

                    Value = value;

                    return true;
                }
            }
        }
    }
}