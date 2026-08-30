namespace Shuttle.Workflow.Events.State.v1;

public class ItemAdded
{
    /// <summary>
    ///     The effective date of the sibling item (with the same name) that gets its <see cref="EffectiveDateEnd" />
    ///     closed to this item's <see cref="EffectiveDate" /> as a mechanical consequence of this append;
    ///     <see langword="null" />
    ///     when there is no such predecessor.
    /// </summary>
    public DateTimeOffset? ClosedPredecessorEffectiveDate { get; set; }

    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset EffectiveDateEnd { get; set; }
    public string Name { get; set; } = string.Empty;
    public StateItemType Type { get; set; }
    public string? Value { get; set; }
}