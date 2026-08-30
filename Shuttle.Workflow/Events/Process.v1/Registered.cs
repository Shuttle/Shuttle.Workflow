namespace Shuttle.Workflow.Events.Process.v1;

public class Registered
{
    public DateTimeOffset DateRegistered { get; set; }
    public string? Description { get; set; }
    public string? Key { get; set; }
    public string Name { get; set; } = string.Empty;
}