namespace Shuttle.Workflow.Messages.v1;

public class CompleteProcessMessage
{
    public DateTimeOffset? DeferredTill { get; set; }
    public Guid MessageId { get; set; }
    public Guid ProcessId { get; set; }
}