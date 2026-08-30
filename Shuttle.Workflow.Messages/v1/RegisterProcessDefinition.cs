namespace Shuttle.Workflow.Messages.v1;

public class RegisterProcessDefinition
{
    public string Description { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}