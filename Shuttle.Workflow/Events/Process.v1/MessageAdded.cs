namespace Shuttle.Workflow.Events.Process.v1;

public class MessageAdded
{
    public TimeSpan? InvokeTimeout { get; set; }
    public Guid MessageId { get; set; }
    public int SequenceNumber { get; set; }
    public string TypeName { get; set; } = string.Empty;
}