namespace Shuttle.Workflow.Messages.v1;

public class ProcessCompleted
{
    public DateTimeOffset DateCompleted { get; set; }
    public Guid Id { get; set; }
}