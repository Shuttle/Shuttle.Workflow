namespace Shuttle.Workflow.Events.Process.v1;

public class ContinuationSet
{
    public Guid MessageId { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public Guid Token { get; set; }
}