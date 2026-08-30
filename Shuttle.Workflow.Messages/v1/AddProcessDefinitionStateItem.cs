namespace Shuttle.Workflow.Messages.v1;

public class AddProcessDefinitionStateItem
{
    public string Name { get; set; } = string.Empty;
    public Guid ProcessDefinitionId { get; set; }
    public string Type { get; set; } = string.Empty;
}