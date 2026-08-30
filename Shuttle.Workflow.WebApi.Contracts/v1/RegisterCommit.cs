namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class RegisterCommit
{
    public DateTimeOffset? DateCommitted { get; set; }
    public string Key { get; set; } = string.Empty;
}