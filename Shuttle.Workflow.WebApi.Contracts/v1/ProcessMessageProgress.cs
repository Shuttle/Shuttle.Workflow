namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class ProcessMessageProgress
{
    public int ItemsCompleted { get; set; }
    public int? ItemsTotal { get; set; }
}
