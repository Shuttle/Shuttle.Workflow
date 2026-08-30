using Refit;
using Shuttle.Workflow.WebApi.Contracts;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.RestClient.v1;

public interface IStateApi
{
    [Get("/v1/states/{stateId}")]
    Task<IApiResponse<State>> GetAsync(Guid stateId, CancellationToken cancellationToken = default);

    [Patch("/v1/states/{stateId}")]
    Task<IApiResponse> PatchAsync(Guid stateId, List<State.Item> stateItems, CancellationToken cancellationToken = default);

    [Patch("/v1/states/{stateId}/delete")]
    Task<IApiResponse> PatchDeleteAsync(Guid stateId, List<string> stateItemNames, CancellationToken cancellationToken = default);

    [Patch("/v1/states/{stateId}/expire")]
    Task<IApiResponse> PatchExpireAsync(Guid stateId, List<State.Expiry> stateItemExpiries, CancellationToken cancellationToken = default);

    [Post("/v1/states")]
    Task<IApiResponse<Registered>> PostAsync(State state, CancellationToken cancellationToken = default);

    [Post("/v1/states/search")]
    Task<IApiResponse<IEnumerable<State>>> PostSearchAsync(State.Specification specification, CancellationToken cancellationToken = default);
}