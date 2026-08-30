namespace Shuttle.Workflow.Messages.v1;

public class UpdateState
{
    public Guid Id { get; set; }
    public List<StateItemValue> Items { get; set; } = [];
    public string Key { get; set; } = string.Empty;
}