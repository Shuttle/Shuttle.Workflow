namespace Shuttle.Workflow.SqlServer;

public class WorkflowSqlServerOptions
{
    public const string SectionName = "Shuttle:Workflow:SqlServer";
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public string ConnectionString { get; set; } = string.Empty;
}