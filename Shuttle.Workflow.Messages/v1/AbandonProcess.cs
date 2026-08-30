namespace Shuttle.Workflow.Messages.v1;

public class AbandonProcess
{
    public string Message { get; set; } = string.Empty;
    public Guid ProcessId { get; set; }
}