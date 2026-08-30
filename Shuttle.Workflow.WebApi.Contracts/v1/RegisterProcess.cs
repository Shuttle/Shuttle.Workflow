namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class RegisterProcess
{
    public DateTimeOffset? DeferredTill { get; set; }
    public string? Description { get; set; }
    public string? Key { get; set; }
    public List<Process.Message> Messages { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? OverdueAt { get; set; }
    public List<State.Item> StateItems { get; set; } = [];
    public bool Wait { get; set; }
}