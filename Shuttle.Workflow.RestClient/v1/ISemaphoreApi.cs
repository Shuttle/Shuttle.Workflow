using Refit;
using Shuttle.Workflow.WebApi.Contracts;
using Semaphore = Shuttle.Workflow.WebApi.Contracts.v1.Semaphore;

namespace Shuttle.Workflow.RestClient.v1;

public interface ISemaphoreApi
{
    [Delete("/v1/semaphores/{id}")]
    Task<IApiResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    [Post("/v1/semaphores")]
    Task<IApiResponse<Registered>> PostAsync(Semaphore semaphore, CancellationToken cancellationToken = default);
}