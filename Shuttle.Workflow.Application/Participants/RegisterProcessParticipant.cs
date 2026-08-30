using Shuttle.Mediator;
using Shuttle.Recall;

namespace Shuttle.Workflow.Application;

public class RegisterProcess(Guid processId, Guid stateId, string name, string? key, string? description, DateTimeOffset? deferredTill, DateTimeOffset? overdueAt, List<RegisterProcess.MessageDefinition> messages, List<RegisterProcess.StateItemValue> stateItems)
{
    public DateTimeOffset? DeferredTill { get; } = deferredTill;
    public string? Description { get; } = description;
    public string? Key { get; } = key;
    public List<MessageDefinition> Messages { get; } = messages;
    public string Name { get; } = name;
    public DateTimeOffset? OverdueAt { get; } = overdueAt;
    public Guid ProcessId { get; } = processId;
    public Guid StateId { get; } = stateId;
    public List<StateItemValue> StateItems { get; } = stateItems;

    public class MessageDefinition
    {
        public TimeSpan? InvokeTimeout { get; set; }
        public int SequenceNumber { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    public class StateItemValue
    {
        public DateTimeOffset EffectiveDate { get; set; } = DateTimeOffset.UnixEpoch;
        public string Name { get; set; } = string.Empty;
        public StateItemType Type { get; set; }
        public string? Value { get; set; }
    }
}

public class RegisterProcessParticipant(IEventStore eventStore) : IParticipant<RegisterProcess>
{
    public async Task HandleAsync(RegisterProcess message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var processStream = (await eventStore.GetAsync(message.ProcessId, cancellationToken: cancellationToken)).MustBeEmpty();
        var process = processStream.Get<Process>();

        processStream.Add(process.Register(message.Name, message.Key, message.Description));

        foreach (var messageDefinition in message.Messages.OrderBy(item => item.SequenceNumber))
        {
            processStream.Add(process.AddMessage(new(Guid.NewGuid(), messageDefinition.TypeName, messageDefinition.SequenceNumber, messageDefinition.InvokeTimeout)));
        }

        if (message.DeferredTill.HasValue)
        {
            var deferred = process.Defer(message.DeferredTill.Value);

            if (deferred != null)
            {
                processStream.Add(deferred);
            }
        }

        if (message.OverdueAt.HasValue)
        {
            processStream.Add(process.WithOverdueAt(message.OverdueAt.Value));
        }

        await eventStore.SaveAsync(processStream, cancellationToken: cancellationToken);

        var stateStream = (await eventStore.GetAsync(message.StateId, cancellationToken: cancellationToken)).MustBeEmpty();
        var state = stateStream.Get<State>();

        stateStream.Add(state.Register($"[process]:{message.ProcessId}"));

        foreach (var stateItem in message.StateItems)
        {
            var itemAdded = state.AddItem(new(stateItem.Name, stateItem.Value, stateItem.Type, stateItem.EffectiveDate));

            if (itemAdded != null)
            {
                stateStream.Add(itemAdded);
            }
        }

        await eventStore.SaveAsync(stateStream, cancellationToken: cancellationToken);
    }
}