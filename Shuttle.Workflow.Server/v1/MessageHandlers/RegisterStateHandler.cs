using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Workflow.Messages.v1;

namespace Shuttle.Workflow.Server.v1.MessageHandlers;

public class RegisterStateHandler(IMediator mediator) : IMessageHandler<RegisterState>
{
    public async Task HandleAsync(RegisterState message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await mediator.SendAsync(new Application.RegisterState(message.Id, message.Key, message.Items.Select(ToApplicationStateItem).ToList()), cancellationToken);
    }

    internal static Application.RegisterState.StateItemValue ToApplicationStateItem(StateItemValue item)
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
}