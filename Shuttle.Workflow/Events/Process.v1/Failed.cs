namespace Shuttle.Workflow.Events.Process.v1;

public class Failed
{
    public DateTimeOffset DateCompleted { get; set; }
    public string Message { get; set; } = string.Empty;
}