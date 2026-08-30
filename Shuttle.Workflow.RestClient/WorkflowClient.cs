using Refit;
using Shuttle.Workflow.RestClient.v1;

namespace Shuttle.Workflow.RestClient;

public class WorkflowClient : IWorkflowClient
{
    public WorkflowClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        Processes = RestService.For<IProcessApi>(httpClient);
        States = RestService.For<IStateApi>(httpClient);
        Semaphores = RestService.For<ISemaphoreApi>(httpClient);
    }

    public IProcessApi Processes { get; }
    public IStateApi States { get; }
    public ISemaphoreApi Semaphores { get; }
}