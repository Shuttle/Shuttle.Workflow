using Shuttle.Workflow.RestClient.v1;

namespace Shuttle.Workflow.RestClient;

public interface IWorkflowClient
{
    IProcessApi Processes { get; }
    ISemaphoreApi Semaphores { get; }
    IStateApi States { get; }
}