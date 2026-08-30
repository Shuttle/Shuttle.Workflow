namespace Shuttle.Workflow.Messages.v1;

public class AddProcessMessage
{
    public Guid ProcessId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}