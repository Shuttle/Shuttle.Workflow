namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class ProcessContinuation
{
    public Guid MessageId { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public Guid Token { get; set; }
}