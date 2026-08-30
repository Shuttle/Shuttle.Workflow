namespace Shuttle.Workflow.Events.Process.v1;

public class MessageCompleted
{
    public DateTimeOffset DateCompleted { get; set; }
    public Guid MessageId { get; set; }
}