using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Access.AspNetCore;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi.Contracts;
using RegisterState = Shuttle.Workflow.Application.RegisterState;

namespace Shuttle.Workflow.WebApi.Endpoints;

public static class StateEndpoints
{
    private static async Task<IResult> Get(IStateQuery stateQuery, Guid stateId)
    {
        var state = (await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddId(stateId).IncludeItems())).FirstOrDefault();

        return state == null ? Results.NotFound() : Results.Ok(Map(state));
    }

    public static Contracts.v1.State Map(SqlServer.Models.State state)
    {
        return new()
        {
            Id = state.Id,
            Key = state.Key,
            Items = state.Items
                .OrderBy(item => item.Name)
                .ThenByDescending(item => item.EffectiveDate)
                .Select(item => new Contracts.v1.State.Item
                {
                    Name = item.Name,
                    Type = item.Type.ToString(),
                    Value = item.StringValue,
                    EffectiveDate = item.EffectiveDate,
                    EffectiveDateEnd = item.EffectiveDateEnd,
                    DateRegistered = item.DateRegistered
                }).ToList()
        };
    }

    private static async Task<IResult> Patch(MessageDispatcher messageDispatcher, IStateQuery stateQuery, Guid stateId, List<Contracts.v1.State.Item> stateItems, CancellationToken cancellationToken)
    {
        if ((await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddId(stateId), cancellationToken)).FirstOrDefault() == null)
        {
            return Results.NotFound();
        }

        var items = new List<StateItemValue>();

        foreach (var stateItem in stateItems)
        {
            if (!Enum.TryParse<StateItemType>(stateItem.Type, out _))
            {
                return Results.BadRequest($"Item with name '{stateItem.Name} has an unknown type '{stateItem.Type}'.");
            }

            items.Add(new() { Name = stateItem.Name, Type = stateItem.Type, Value = stateItem.Value, EffectiveDate = stateItem.EffectiveDate, EffectiveDateEnd = stateItem.EffectiveDateEnd });
        }

        await messageDispatcher.DispatchAsync(
            () => new AddStateItems { StateId = stateId, Items = items },
            () => new Application.AddStateItems(stateId, items.Select(ToApplicationStateItem).ToList()),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchDelete(MessageDispatcher messageDispatcher, IStateQuery stateQuery, Guid stateId, List<string> stateItemNames, CancellationToken cancellationToken)
    {
        if ((await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddId(stateId), cancellationToken)).FirstOrDefault() == null)
        {
            return Results.NotFound();
        }

        await messageDispatcher.DispatchAsync(
            () => new RemoveStateItems { StateId = stateId, Names = stateItemNames },
            () => new Application.RemoveStateItems(stateId, stateItemNames),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> PatchExpire(MessageDispatcher messageDispatcher, IStateQuery stateQuery, Guid stateId, List<Contracts.v1.State.Expiry> stateItemExpiries, CancellationToken cancellationToken)
    {
        if ((await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddId(stateId), cancellationToken)).FirstOrDefault() == null)
        {
            return Results.NotFound();
        }

        foreach (var stateItemExpiry in stateItemExpiries)
        {
            if (string.IsNullOrWhiteSpace(stateItemExpiry.Name))
            {
                return Results.BadRequest("A state item expiry requires a name.");
            }
        }

        var expiries = stateItemExpiries
            .Select(item => new ExpireStateItems.Expiry { Name = item.Name, EffectiveDateEnd = item.EffectiveDateEnd ?? DateTimeOffset.UtcNow })
            .ToList();

        await messageDispatcher.DispatchAsync(
            () => new ExpireStateItems { StateId = stateId, Expiries = expiries },
            () => new Application.ExpireStateItems(stateId, expiries.Select(item => new Application.ExpireStateItems.Expiry { Name = item.Name, EffectiveDateEnd = item.EffectiveDateEnd }).ToList()),
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<object> Post(MessageDispatcher messageDispatcher, IStateQuery stateQuery, [FromBody] Contracts.v1.State model, CancellationToken cancellationToken)
    {
        return await SaveAsync(true, model, stateQuery, messageDispatcher, cancellationToken);
    }

    private static async Task<IResult> PostSearch(IStateQuery stateQuery, [FromBody] Contracts.v1.State.Specification model)
    {
        var specification = new SqlServer.Models.State.Specification();

        if (model.Id.HasValue)
        {
            specification.AddId(model.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            specification.WithKey(model.Key);
        }

        if (!string.IsNullOrWhiteSpace(model.KeyMatch))
        {
            specification.WithKeyMatch(model.KeyMatch);
        }

        if (model.ItemEffectiveDate.HasValue)
        {
            specification.WithItemEffectiveDate(model.ItemEffectiveDate.Value);
        }

        if (model.ItemStartEffectiveDate.HasValue)
        {
            specification.WithItemStartEffectiveDate(model.ItemStartEffectiveDate.Value);
        }

        if (model.ItemEndEffectiveDateExclusive.HasValue)
        {
            specification.WithItemEndEffectiveDateExclusive(model.ItemEndEffectiveDateExclusive.Value);
        }

        foreach (var modelItemMatch in model.ItemMatches)
        {
            specification.WithItemMatch(new(modelItemMatch.Name, modelItemMatch.Operator, modelItemMatch.Value, modelItemMatch.Type));
        }

        if (model.MaximumRows > 0)
        {
            specification.WithMaximumRows(model.MaximumRows);
        }

        return Results.Ok((await stateQuery.SearchAsync(specification.IncludeItems())).Select(Map));
    }

    private static async Task<object> Put(MessageDispatcher messageDispatcher, IStateQuery stateQuery, [FromBody] Contracts.v1.State model, Guid id, CancellationToken cancellationToken)
    {
        model.Id = id;

        return await SaveAsync(false, model, stateQuery, messageDispatcher, cancellationToken);
    }

    private static async Task<object> SaveAsync(bool register, Contracts.v1.State model, IStateQuery stateQuery, MessageDispatcher messageDispatcher, CancellationToken cancellationToken)
    {
        var id = model.Id ?? Guid.NewGuid();

        if (!register && !model.Id.HasValue)
        {
            return Results.BadRequest("A state id is required.");
        }

        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            var existingState = (await stateQuery.SearchAsync(new SqlServer.Models.State.Specification().AddExcludedId(id).WithKey(model.Key), cancellationToken)).FirstOrDefault();

            if (existingState != null)
            {
                return Results.Conflict($"There is already a state entry with key '{model.Key}'.");
            }
        }

        var items = new List<StateItemValue>();

        foreach (var stateItem in model.Items)
        {
            if (!Enum.TryParse<StateItemType>(stateItem.Type, out _))
            {
                return Results.BadRequest($"Could not parse '{stateItem.Type}' value '{stateItem.Value}' for item with name '{stateItem.Name}'.");
            }

            items.Add(new() { Name = stateItem.Name, Type = stateItem.Type, Value = stateItem.Value, EffectiveDate = stateItem.EffectiveDate, EffectiveDateEnd = stateItem.EffectiveDateEnd });
        }

        var applicationItems = items.Select(ToApplicationStateItem).ToList();

        if (register)
        {
            await messageDispatcher.DispatchAsync(
                () => new Messages.v1.RegisterState { Id = id, Key = model.Key, Items = items },
                () => new RegisterState(id, model.Key, applicationItems),
                cancellationToken);
        }
        else
        {
            await messageDispatcher.DispatchAsync(
                () => new UpdateState { Id = id, Key = model.Key, Items = items },
                () => new Application.UpdateState(id, model.Key, applicationItems),
                cancellationToken);
        }

        return Results.Accepted(null, new Registered { Id = id });
    }

    private static RegisterState.StateItemValue ToApplicationStateItem(StateItemValue item)
    {
        return new()
        {
            Name = item.Name,
            Type = Enum.Parse<StateItemType>(item.Type, true),
            Value = item.Value,
            EffectiveDate = item.EffectiveDate,
            EffectiveDateEnd = item.EffectiveDateEnd
        };
    }

    extension(WebApplication app)
    {
        public WebApplication MapStateEndpoints(ApiVersionSet versionSet)
        {
            var apiVersion1 = new ApiVersion(1, 0);

            app.MapPatch("/v{version:apiVersion}/states/{stateId:Guid}", Patch)
                .RequirePermission(Permissions.States.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/states/{stateId:Guid}/delete", PatchDelete)
                .RequirePermission(Permissions.States.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPatch("/v{version:apiVersion}/states/{stateId:Guid}/expire", PatchExpire)
                .RequirePermission(Permissions.States.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound);

            app.MapPost("/v{version:apiVersion}/states", Post)
                .RequirePermission(Permissions.States.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPut("/v{version:apiVersion}/states/{id:guid}", Put)
                .RequirePermission(Permissions.States.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapGet("/v{version:apiVersion}/states/{stateId:Guid}", Get)
                .RequirePermission(Permissions.States.View)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/states/search", PostSearch)
                .RequirePermission(Permissions.States.View)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            return app;
        }
    }
}