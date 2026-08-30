namespace Shuttle.Workflow.Events.ProcessDefinition.v1;

public class StateItemAdded
{
    public string Name { get; set; } = string.Empty;
    public StateItemType Type { get; set; }
}