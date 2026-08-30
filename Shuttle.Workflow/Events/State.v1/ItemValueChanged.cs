namespace Shuttle.Workflow.Events.State.v1;

public class ItemValueChanged
{
    public DateTimeOffset EffectiveDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public StateItemType Type { get; set; }
    public string? Value { get; set; }
}