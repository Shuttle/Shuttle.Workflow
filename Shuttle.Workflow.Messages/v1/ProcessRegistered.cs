namespace Shuttle.Workflow.Messages.v1;

public class ProcessRegistered
{
    public Guid Id { get; set; }
    public string? Key { get; set; }
    public string Name { get; set; } = string.Empty;
}