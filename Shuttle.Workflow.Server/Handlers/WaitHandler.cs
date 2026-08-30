using Microsoft.Extensions.Logging;
using Shuttle.Hopper;
using Shuttle.Recall;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.Handlers;

public class WaitHandler(ILogger<WaitHandler> logger, IBus bus, IEventStore eventStore) : IContextMessageHandler<Wait>
{
    private readonly string _fullName = typeof(Wait).FullName ?? throw new ApplicationException("Could not get full name of type 'Wait'.");

    public async Task HandleAsync(IHandlerContext<Wait> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var processId = context.GetProcessId();
        var stream = await eventStore.GetAsync(processId, cancellationToken: cancellationToken);
        var process = stream.Get<Process>();

        logger.LogInformation($"[process] : id = '{processId}' / status = '{process.Status}' / status message = '{process.StatusMessage}'");

        var message = process.GetNextMessage();

        if (message == null)
        {
            stream.Add(process.Complete());

            await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);

            return;
        }

        var information = $"process.StatusMessage = '{process.StatusMessage}' / message.Id = '{message.Id}'";

        logger.LogInformation($"[check] : {information}");

        if ((process.StatusMessage ?? string.Empty).Equals($"[continue]:{message.Id}"))
        {
            logger.LogInformation($"[check/complete] : {information}");

            stream.Add(process.CompleteMessage(message.Id));

            await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);

            await bus.SendAsync(new SendProcessMessage(), builder => builder.WithCorrelationId($"{processId}"), cancellationToken);
        }
        else
        {
            logger.LogInformation($"[check/waiting] : {information}");

            stream.Add(process.Wait(_fullName));

            await eventStore.SaveAsync(stream, cancellationToken: cancellationToken);
        }
    }
}