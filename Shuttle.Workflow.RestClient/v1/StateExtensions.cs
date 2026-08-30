using System.Globalization;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient.v1;

public static class StateExtensions
{
    private static readonly Type DateTimeType = typeof(DateTime);
    private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);
    private static readonly Type GuidType = typeof(Guid);

    public static State AddItem(this State state, string name, string? value, string type, DateTimeOffset effectiveDate)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentNullException(nameof(type));
        }

        var stateItem = state.FindItem(name, effectiveDate);

        if (stateItem != null &&
            (stateItem.Value ?? string.Empty).Equals(value ?? string.Empty, StringComparison.InvariantCultureIgnoreCase))
        {
            return state;
        }

        var item = new State.Item
        {
            Name = name,
            Type = type,
            Value = value,
            EffectiveDate = effectiveDate
        };

        state.Splice(item);

        state.Items.Add(item);

        return state;
    }

    public static State Clone(this State state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new()
        {
            Id = state.Id,
            Key = state.Key,
            Items = state.Items.Select(item => item.Clone()).ToList()
        };
    }

    public static State.Item Clone(this State.Item item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return new()
        {
            Name = item.Name,
            Type = item.Type,
            Value = item.Value,
            EffectiveDate = item.EffectiveDate,
            EffectiveDateEnd = item.EffectiveDateEnd
        };
    }

    public static bool ContainsItem(this State state, string name, DateTimeOffset? at = null)
    {
        return state.FindItem(name, at) != null;
    }

    public static State.Item? FindItem(this State state, string name, DateTimeOffset? at = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return state.Items
            .OrderByDescending(item => item.EffectiveDate)
            .FirstOrDefault(item => item.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && item.EffectiveDate <= atDateTimeOffset && item.EffectiveDateEnd > atDateTimeOffset);
    }

    public static State.Item GetItem(this State state, string name, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return state.FindItem(name, atDateTimeOffset) ?? throw new KeyNotFoundException($"Item with name '{name}' not found in state with key '{state.Key}' as at '{atDateTimeOffset:O}'");
    }

    public static List<State.Item> GetItems(this State state, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return state.Items
            .Where(item => item.EffectiveDate <= atDateTimeOffset && item.EffectiveDateEnd > atDateTimeOffset)
            .ToList();
    }

    public static T? GetValue<T>(this State state, string name, DateTimeOffset? at = null)
    {
        var atDateTimeOffset = at ?? DateTimeOffset.UtcNow;

        return (state.FindItem(name, atDateTimeOffset) ?? throw new KeyNotFoundException($"Item with name '{name}' not found in state with key '{state.Key}' as at '{atDateTimeOffset:O}'")).GetValue<T>();
    }

    public static T? GetValue<T>(this State.Item stateItem)
    {
        ArgumentNullException.ThrowIfNull(stateItem);

        if (stateItem.Value == null)
        {
            return default;
        }

        var type = typeof(T);

        if (type == DateTimeType)
        {
            // 'AssumeUniversal' does not shift the value -- it only guarantees 'DateTimeKind.Utc' on the way
            // out.  Without it a stored string carrying no designator parsed back as 'Unspecified', and the
            // implicit 'DateTime' -> 'DateTimeOffset' conversion would then stamp it with the *local* offset
            // wherever a caller feeds the result into an 'EffectiveDate' or 'DeferredTill'.
            if (!DateTime.TryParse(stateItem.Value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            {
                throw new ArgumentException($"Item with name '{stateItem.Name}' has an invalid `DateTime` value of '{stateItem.Value}'.");
            }

            return (T)(object)result;
        }

        if (type == DateTimeOffsetType)
        {
            // 'AssumeUniversal' keeps parity with the 'DateTime' branch above, which leaves a designator-less
            // string unshifted; without it 'DateTimeOffset.TryParse' would apply the local offset.
            if (!DateTimeOffset.TryParse(stateItem.Value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
            {
                throw new ArgumentException($"Item with name '{stateItem.Name}' has an invalid `DateTimeOffset` value of '{stateItem.Value}'.");
            }

            return (T)(object)result;
        }

        if (type == GuidType)
        {
            if (!Guid.TryParse(stateItem.Value, out var result))
            {
                throw new ArgumentException($"Item with name '{stateItem.Name}' has an invalid `Guid` value of '{stateItem.Value}'.");
            }

            return (T)(object)result;
        }

        return (T)Convert.ChangeType(stateItem.Value, Nullable.GetUnderlyingType(type) ?? type);
    }

    // Mirrors 'Shuttle.Workflow.State.Splice': the item's end comes from its successor and its
    // predecessor's end from the item.  Ends only ever move earlier, so an item expired without a replacement
    // is never silently re-opened by a later back-dated insert.
    private static void Splice(this State state, State.Item item)
    {
        var siblings = state.Items
            .Where(candidate => candidate.Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var boundary = siblings
            .Where(candidate => candidate.EffectiveDate > item.EffectiveDate)
            .OrderBy(candidate => candidate.EffectiveDate)
            .FirstOrDefault()?.EffectiveDate ?? DateTimeOffset.MaxValue;

        if (item.EffectiveDateEnd > boundary)
        {
            item.EffectiveDateEnd = boundary;
        }

        var predecessor = siblings
            .Where(candidate => candidate.EffectiveDate < item.EffectiveDate)
            .OrderByDescending(candidate => candidate.EffectiveDate)
            .FirstOrDefault();

        if (predecessor == null || predecessor.EffectiveDateEnd <= item.EffectiveDate)
        {
            return;
        }

        predecessor.EffectiveDateEnd = item.EffectiveDate;
    }
}