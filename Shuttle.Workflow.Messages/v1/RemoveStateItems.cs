namespace Shuttle.Workflow.Messages.v1;

public class RemoveStateItems
{
    public List<string> Names { get; set; } = [];
    public Guid StateId { get; set; }
}