namespace Shuttle.Workflow.Messages.v1;

public class RegisterState
{
    public Guid Id { get; set; }
    public List<StateItemValue> Items { get; set; } = [];
    public string Key { get; set; } = string.Empty;
}