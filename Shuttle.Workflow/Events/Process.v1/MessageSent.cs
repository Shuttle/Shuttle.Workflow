namespace Shuttle.Workflow.Events.Process.v1;

public class MessageSent
{
    public DateTimeOffset DateSent { get; set; }
    public Guid MessageId { get; set; }
}