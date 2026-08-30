namespace Shuttle.Workflow.Messages.v1;

public class FailProcess
{
    public string Message { get; set; } = string.Empty;
    public Guid ProcessId { get; set; }
}