namespace Shuttle.Workflow.Messages.v1;

public class DeferProcess
{
    public DateTimeOffset DeferredTill { get; set; }
    public Guid ProcessId { get; set; }
}