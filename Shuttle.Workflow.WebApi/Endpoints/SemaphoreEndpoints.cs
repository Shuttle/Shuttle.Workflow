using System.Transactions;
using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shuttle.Access.AspNetCore;
using Shuttle.Mediator;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi.Contracts;

namespace Shuttle.Workflow.WebApi.Endpoints;

public static class SemaphoreEndpoints
{
    private static async Task<IResult> Delete(WorkflowDbContext dbContext, ISemaphoreRepository semaphoreRepository, Guid id, CancellationToken cancellationToken)
    {
        await semaphoreRepository.RemoveAsync(id, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static Contracts.v1.Semaphore Map(SqlServer.Models.Semaphore semaphore)
    {
        return new()
        {
            Id = semaphore.Id,
            Key = semaphore.Key,
            Owner = semaphore.Owner,
            DateRegistered = semaphore.DateRegistered,
            ExpiresAt = semaphore.ExpiresAt
        };
    }

    private static async Task<IResult> Post(ILogger<Semaphore> logger, WorkflowDbContext dbContext, ISemaphoreOptionsResolver semaphoreOptionsResolver, ISemaphoreRepository semaphoreRepository, IMediator mediator, [FromBody] Contracts.v1.Semaphore model, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var options = semaphoreOptionsResolver.Resolve(model.Key);

        using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        Guid? id;

        try
        {
            var semaphore = await semaphoreRepository.FindAsync(model.Key, cancellationToken)
                            ?? new Semaphore(model.Key, model.Owner, DateTimeOffset.UtcNow, model.ExpiresAt);

            if (!semaphore.Owner.Equals(model.Owner, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!semaphore.HasExpired(DateTimeOffset.UtcNow) || (!options?.RemoveWhenExpired ?? false))
                {
                    return Results.Conflict();
                }

                semaphore.Reassign(model.Owner, DateTimeOffset.UtcNow);
            }

            var expiresAt = model.ExpiresAt ?? (options != null ? now.Add(options.Lifetime) : null);

            if (expiresAt.HasValue)
            {
                semaphore.ExpireAt(expiresAt.Value);
            }

            if (options != null && !semaphore.SatisfiesMinimumLifetime(DateTimeOffset.UtcNow, options.MinimumLifetime))
            {
                return Results.BadRequest($"Semaphore must have a lifetime of at least '{options.MinimumLifetime}'.");
            }

            id = await semaphoreRepository.SaveAsync(semaphore, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            if ((ex.InnerException?.Message ?? string.Empty).Contains("duplicate", StringComparison.InvariantCultureIgnoreCase))
            {
                return Results.Conflict();
            }

            throw;
        }

        tx.Complete();

        return id.HasValue ? Results.Ok(new Registered { Id = id.Value }) : Results.Conflict();
    }

    private static async Task<IResult> PostSearch(ISemaphoreQuery semaphoreQuery, [FromBody] Contracts.v1.Semaphore.Specification model)
    {
        var specification = new SqlServer.Models.Semaphore.Specification();

        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            specification.WithKey(model.Key);
        }

        if (!string.IsNullOrWhiteSpace(model.KeyMatch))
        {
            specification.WithKeyMatch(model.KeyMatch);
        }

        if (!string.IsNullOrWhiteSpace(model.Owner))
        {
            specification.WithOwner(model.Owner);
        }

        if (!string.IsNullOrWhiteSpace(model.OwnerMatch))
        {
            specification.WithOwnerMatch(model.OwnerMatch);
        }

        if (model.FromExpiresAtInclusive.HasValue)
        {
            specification.WithFromExpiresAtInclusive(model.FromExpiresAtInclusive.Value);
        }

        if (model.ToExpiresAtExclusive.HasValue)
        {
            specification.WithToExpiresAtExclusive(model.ToExpiresAtExclusive.Value);
        }

        if (model.MaximumRows > 0)
        {
            specification.WithMaximumRows(model.MaximumRows);
        }

        return Results.Ok((await semaphoreQuery.SearchAsync(specification)).Select(Map).ToList());
    }

    extension(WebApplication app)
    {
        public WebApplication MapSemaphoreEndpoints(ApiVersionSet versionSet)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var apiVersion1 = new ApiVersion(1, 0);

            app.MapPost("/v{version:apiVersion}/semaphores/search", PostSearch)
                .RequirePermission(Permissions.Semaphores.View)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapPost("/v{version:apiVersion}/semaphores", Post)
                .RequirePermission(Permissions.Semaphores.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            app.MapDelete("/v{version:apiVersion}/semaphores/{id:guid}", Delete)
                .RequirePermission(Permissions.Semaphores.Manage)
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(apiVersion1);

            return app;
        }
    }
}