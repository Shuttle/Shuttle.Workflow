using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class AddProcessDefinitionMessageHandler(IMediator mediator) : IMessageHandler<AddProcessDefinitionMessage>
{
    public async Task HandleAsync(AddProcessDefinitionMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.AddProcessDefinitionMessage(message.ProcessDefinitionId, message.TypeName, message.SequenceNumber, message.InvokeTimeout), cancellationToken);
    }
}