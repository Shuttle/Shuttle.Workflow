namespace Shuttle.Workflow.Messages.v1;

public class SetProcessMessageProgress
{
    public int ItemsCompleted { get; set; }
    public int? ItemsTotal { get; set; }
    public Guid MessageId { get; set; }
    public Guid ProcessId { get; set; }
}
