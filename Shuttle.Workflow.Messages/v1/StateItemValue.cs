namespace Shuttle.Workflow.Messages.v1;

public class StateItemValue
{
    public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UnixEpoch;
    public DateTimeOffset EffectiveDateEnd { get; set; } = DateTimeOffset.MaxValue;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Value { get; set; }
}