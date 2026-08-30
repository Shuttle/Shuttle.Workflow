using Shuttle.Contract;
using Shuttle.Hopper;

namespace Shuttle.Workflow.Server;

public static class HandlerContextExtensions
{
    extension(IHandlerContext context)
    {
        public Guid GetProcessId()
        {
            ArgumentNullException.ThrowIfNull(context);

            var transportMessage = Guard.AgainstNull(context.State.GetTransportMessage());

            return !Guid.TryParse(transportMessage.CorrelationId, out var processId)
                ? throw new InvalidOperationException($"Cannot determine the process id since the correlation id value '{transportMessage.CorrelationId}' is not a valid GUID.")
                : processId;
        }
    }
}