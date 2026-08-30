namespace Shuttle.Workflow.Events.Process.v1;

public class Committed
{
    public DateTimeOffset DateCommitted { get; set; }
    public string Key { get; set; } = string.Empty;
}