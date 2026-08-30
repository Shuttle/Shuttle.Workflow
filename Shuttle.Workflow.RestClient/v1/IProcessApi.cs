using Refit;
using Shuttle.Workflow.WebApi.Contracts;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient.v1;

public interface IProcessApi
{
    [Patch("/v1/processes/{processId}/abandon")]
    Task<IApiResponse> AbandonAsync(Guid processId, ProcessStatus processStatus, CancellationToken cancellationToken = default);

    [Post("/v1/processes/{processId}/commits")]
    Task<IApiResponse> CommitAsync(Guid processId, RegisterCommit commit, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/continue")]
    Task<IApiResponse> ContinueAsync(Guid processId, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/continue/{continuationToken}")]
    Task<IApiResponse> ContinueAsync(Guid processId, Guid continuationToken, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/continue-deferred")]
    Task<IApiResponse> ContinueDeferredAsync(Guid processId, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/defer")]
    Task<IApiResponse> DeferAsync(Guid processId, DeferProcess deferProcess, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/fail")]
    Task<IApiResponse> FailAsync(Guid processId, ProcessStatus processStatus, CancellationToken cancellationToken = default);

    [Get("/v1/processes/{processId}")]
    Task<IApiResponse<Process>> GetAsync(Guid processId, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/message-completed/{messageId}")]
    Task<IApiResponse> MessageCompletedAsync(Guid processId, Guid messageId, DeferProcess? deferProcess = null, CancellationToken cancellationToken = default);

    [Patch("/v1/processes")]
    Task<IApiResponse> PatchAsync(Process process, CancellationToken cancellationToken = default);

    [Post("/v1/processes")]
    Task<IApiResponse<Registered>> PostAsync(RegisterProcess message, CancellationToken cancellationToken = default);

    [Post("/v1/processes/search")]
    Task<IApiResponse<IEnumerable<Process>>> PostSearchAsync(Process.Specification specification, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/overdue-at")]
    Task<IApiResponse> SetOverdueAtAsync(Guid processId, OverdueProcess overdueProcess, CancellationToken cancellationToken = default);

    [Patch("/v1/processes/{processId}/wait")]
    Task<IApiResponse<ProcessContinuation>> WaitAsync(Guid processId, ProcessStatus processStatus, CancellationToken cancellationToken = default);
}