namespace Shuttle.Workflow.Events.State.v1;

public class ItemExpired
{
    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset EffectiveDateEnd { get; set; }
    public string Name { get; set; } = string.Empty;
}