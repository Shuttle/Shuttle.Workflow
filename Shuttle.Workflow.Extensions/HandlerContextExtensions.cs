using System.Net;
using Microsoft.Extensions.Logging;
using Shuttle.Contract;
using Shuttle.Hopper;
using Shuttle.Workflow.RestClient;
using Shuttle.Workflow.WebApi.Contracts.v1;

namespace Shuttle.Workflow.Extensions;

public static class HandlerContextExtensions
{
    extension<T>(IHandlerContext<T> context) where T : class
    {
        public async Task<Guid?> GetSemaphoreIdAsync(ILogger logger, IWorkflowClient workflowClient, string key, bool defer = true)
        {
            ArgumentNullException.ThrowIfNull(logger);

            var semaphoreId = await context.GetSemaphoreIdAsync(workflowClient, key, defer);

            // Do not use ternary since logging message cannot vary.
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (semaphoreId.HasValue)
            {
                logger.LogDebug("Acquired semaphore with key '{Key}'.", key);
            }
            else
            {
                logger.LogDebug("Semaphore with key '{Key}' has already been acquired by another owner.", key);
            }

            return semaphoreId;
        }

        public async Task<Guid?> GetSemaphoreIdAsync(IWorkflowClient workflowClient, string key, CancellationToken cancellationToken = default)
        {
            return await context.GetSemaphoreIdAsync(workflowClient, key, true, cancellationToken);
        }

        public async Task<Guid?> GetSemaphoreIdAsync(IWorkflowClient workflowClient, string key, bool defer = true, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var semaphoreResponse = await workflowClient.Semaphores.PostAsync(new()
            {
                Key = key,
                Owner = Guard.AgainstNull(context.State.GetTransportMessage()).CorrelationId
            }, cancellationToken);

            if (semaphoreResponse.StatusCode == HttpStatusCode.Conflict)
            {
                if (!defer)
                {
                    return null;
                }

                await context.SendAsync(context.Message, builder =>
                {
                    var ignoreTillDate = DateTimeOffset.UtcNow.AddSeconds(Random.Shared.Next(30, 120));

                    builder.ToSelf().DeferUntil(ignoreTillDate);
                }, cancellationToken);

                return null;
            }

            if (!semaphoreResponse.IsSuccessStatusCode || semaphoreResponse.Content == null)
            {
                throw new ApplicationException($"Could not acquire semaphore with key '{key}'.  Error: {semaphoreResponse.Error}");
            }

            return semaphoreResponse.Content.Id;
        }
    }

    extension(IHandlerContext context)
    {
        public async Task AbandonProcessAsync(IWorkflowClient workflowClient, Guid processId, string message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var response = await workflowClient.Processes.AbandonAsync(processId, new() { Message = message }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException("Could not abandon process.");
            }
        }

        public async Task CompleteMessageAsync(IWorkflowClient workflowClient, Guid processId, CancellationToken cancellationToken)
        {
            await context.CompleteMessageAsync(workflowClient, processId, null, cancellationToken);
        }

        public async Task CompleteMessageAsync(IWorkflowClient workflowClient, Guid processId, DeferProcess? deferProcess = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var messageId = context.GetProcessMessageId();
            var messageCompletedResponse = await workflowClient.Processes.MessageCompletedAsync(processId, messageId, deferProcess, cancellationToken);

            if (!messageCompletedResponse.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Could not complete message with id '{messageId}'.");
            }
        }

        public async Task<Process> GetProcessAsync(IWorkflowClient workflowClient)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var process = await workflowClient.Processes.GetAsync(context.GetProcessId());

            return process.Content ?? throw new ApplicationException("Could not retrieve process from Workflow.");
        }

        public Guid GetProcessId()
        {
            ArgumentNullException.ThrowIfNull(context);

            var transportMessage = Guard.AgainstNull(context.State.GetTransportMessage());
            return !Guid.TryParse(transportMessage.CorrelationId, out var processId)
                ? throw new InvalidOperationException($"Cannot determine the process id since the correlation id value '{transportMessage.CorrelationId}' is not a valid GUID.")
                : processId;
        }

        public Guid GetProcessMessageId()
        {
            ArgumentNullException.ThrowIfNull(context);

            var processMessageIdHeader = Guard.AgainstNull(context.State.GetTransportMessage()).Headers.FirstOrDefault(item => item.Key.Equals("ProcessMessageId", StringComparison.InvariantCulture));

            if (processMessageIdHeader == null)
            {
                throw new InvalidOperationException("Could not find a transport message header with key 'ProcessMessageId'.");
            }

            return !Guid.TryParse(processMessageIdHeader.Value, out var processMessageId)
                ? throw new InvalidOperationException($"Cannot determine the process message id since the 'ProcessMessageId' transport header value '{processMessageIdHeader.Value}' is not a valid GUID.")
                : processMessageId;
        }

        public async Task SetOverdueAtAsync(IWorkflowClient workflowClient, Guid processId, DateTimeOffset overdueAt, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var response = await workflowClient.Processes.SetOverdueAtAsync(processId, new() { OverdueAt = overdueAt }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException("Could not set process overdue at.");
            }
        }

        public async Task SetProgressAsync(IWorkflowClient workflowClient, Guid processId, int? itemsTotal, int itemsCompleted, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(workflowClient);

            var messageId = context.GetProcessMessageId();
            var response = await workflowClient.Processes.SetProgressAsync(processId, messageId, new() { ItemsTotal = itemsTotal, ItemsCompleted = itemsCompleted }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Could not set progress for message with id '{messageId}'.");
            }
        }
    }
}