namespace Shuttle.Workflow;

public class WorkflowOptions
{
    public const string SectionName = "Shuttle:Workflow:SqlServer";

    public List<SemaphoreOptions> Semaphores { get; set; } = [];
}

public class SemaphoreOptions
{
    public string KeyRegex { get; set; } = ".*";
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan MinimumLifetime { get; set; } = TimeSpan.FromSeconds(5);
    public bool RemoveWhenExpired { get; set; } = true;
}