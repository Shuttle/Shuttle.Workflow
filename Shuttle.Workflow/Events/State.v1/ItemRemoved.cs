namespace Shuttle.Workflow.Events.State.v1;

public class ItemRemoved
{
    public DateTimeOffset EffectiveDate { get; set; }
    public string Name { get; set; } = string.Empty;
}