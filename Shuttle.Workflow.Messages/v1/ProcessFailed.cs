namespace Shuttle.Workflow.Messages.v1;

public class ProcessFailed
{
    public DateTimeOffset DateCompleted { get; set; }
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
}