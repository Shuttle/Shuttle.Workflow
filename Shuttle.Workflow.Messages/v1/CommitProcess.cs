namespace Shuttle.Workflow.Messages.v1;

public class CommitProcess
{
    public DateTimeOffset DateCommitted { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid ProcessId { get; set; }
}