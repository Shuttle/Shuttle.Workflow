namespace Shuttle.Workflow.Messages.v1;

public class AddProcessDefinitionMessage
{
    public TimeSpan? InvokeTimeout { get; set; }
    public Guid ProcessDefinitionId { get; set; }
    public int SequenceNumber { get; set; }
    public string TypeName { get; set; } = string.Empty;
}