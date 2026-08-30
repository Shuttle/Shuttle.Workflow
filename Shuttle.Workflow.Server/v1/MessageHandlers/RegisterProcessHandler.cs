using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class RegisterProcessHandler(IMediator mediator) : IMessageHandler<RegisterProcess>
{
    public async Task HandleAsync(RegisterProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.RegisterProcess(
            message.Id,
            message.StateId,
            message.Name,
            message.Key,
            message.Description,
            message.DeferredTill,
            message.OverdueAt,
            message.Messages.Select(item => new Application.RegisterProcess.MessageDefinition { TypeName = item.TypeName, SequenceNumber = item.SequenceNumber, InvokeTimeout = item.InvokeTimeout }).ToList(),
            message.StateItems.Select(item => new Application.RegisterProcess.StateItemValue { Name = item.Name, Type = Enum.Parse<StateItemType>(item.Type, true), Value = item.Value, EffectiveDate = item.EffectiveDate }).ToList()), cancellationToken);
    }
}