namespace Shuttle.Workflow.Messages.v1;

public class SetProcessOverdueAt
{
    public DateTimeOffset OverdueAt { get; set; }
    public Guid ProcessId { get; set; }
}