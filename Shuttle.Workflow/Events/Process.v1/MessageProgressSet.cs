namespace Shuttle.Workflow.Events.Process.v1;

public class MessageProgressSet
{
    public int ItemsCompleted { get; set; }
    public int? ItemsTotal { get; set; }
    public Guid MessageId { get; set; }
}
