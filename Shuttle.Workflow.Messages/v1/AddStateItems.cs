namespace Shuttle.Workflow.Messages.v1;

public class AddStateItems
{
    public List<StateItemValue> Items { get; set; } = [];
    public Guid StateId { get; set; }
}