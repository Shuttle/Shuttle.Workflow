using Microsoft.Extensions.Logging;
using Shuttle.Hopper;
using Shuttle.Recall;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.Handlers;

public class SendProcessMessageHandler(ILogger<SendProcessMessageHandler> logger, IEventStore eventStore, ITransportMessagePipeline transportMessagePipeline, IDispatchTransportMessagePipeline dispatchTransportMessagePipeline) : IContextMessageHandler<SendProcessMessage>
{
    public async Task HandleAsync(IHandlerContext<SendProcessMessage> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var processId = context.GetProcessId();
        var stream = await eventStore.GetAsync(processId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        logger.LogInformation($"[process] : id = '{processId}' / status = '{process.Status}' / status message = '{process.StatusMessage}'");

        if (process.IsDeferred())
        {
            return;
        }

        var message = process.GetNextMessage();

        if (message == null)
        {
            stream.Add(process.Complete());

            await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);

            return;
        }

        await transportMessagePipeline.ExecuteAsync(new(), builder =>
        {
            builder.WithCorrelationId($"{processId}");

            builder.Headers.Add(new()
            {
                Key = "ProcessMessageId",
                Value = message.Id.ToString()
            });
        }, cancellationToken);

        var transportMessage = transportMessagePipeline.State.GetTransportMessage();

        if (transportMessage == null)
        {
            throw new ApplicationException("Could not create transport message.");
        }

        var pos = message.TypeName.IndexOf(',', StringComparison.Ordinal);

        transportMessage.MessageType = pos > -1 ? message.TypeName[..pos] : message.TypeName;
        transportMessage.AssemblyQualifiedName = message.TypeName;

        if (!process.HasStatus(Process.StatusNames.Started))
        {
            stream.Add(process.Start());
        }

        stream.Add(process.SendMessage(message.Id));

        await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);

        await dispatchTransportMessagePipeline.ExecuteAsync(transportMessage, cancellationToken);
    }
}