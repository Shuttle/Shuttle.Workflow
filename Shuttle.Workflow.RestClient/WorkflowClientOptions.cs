namespace Shuttle.Workflow.RestClient;

public class WorkflowClientOptions
{
    public const string SectionName = "Shuttle:Workflow:Client";

    public Uri? BaseAddress { get; set; }
    public Func<HttpRequestMessage, IServiceProvider, Task>? ConfigureHttpRequestAsync { get; set; }
}