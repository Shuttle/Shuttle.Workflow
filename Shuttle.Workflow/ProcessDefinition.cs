using System.Globalization;
using Shuttle.Workflow.Events.ProcessDefinition.v1;

namespace Shuttle.Workflow;

public class ProcessDefinition
{
    private readonly List<MessageDefinition> _messageDefinitions = [];
    private readonly List<StateItemDefinition> _registrationStateItems = [];
    public string Description { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public MessageAdded AddMessage(string typeName, int sequenceNumber = 0, TimeSpan? invokeTimeout = null)
    {
        var maximumSequenceNumber = _messageDefinitions.Count + 1;

        if (sequenceNumber > maximumSequenceNumber)
        {
            throw new InvalidOperationException($"Message type name '{typeName}' has sequence number {sequenceNumber} but it may not be greater than {maximumSequenceNumber}.");
        }

        var resolvedSequenceNumber = sequenceNumber < 1 ? maximumSequenceNumber : sequenceNumber;

        var resequenced = sequenceNumber < 1
            ? []
            : _messageDefinitions
                .Where(messageDefinition => messageDefinition.SequenceNumber >= resolvedSequenceNumber)
                .Select(messageDefinition => new MessageAdded.ResequencedMessage { TypeName = messageDefinition.TypeName, SequenceNumber = messageDefinition.SequenceNumber + 1 })
                .ToList();

        return On(new MessageAdded
        {
            TypeName = typeName,
            SequenceNumber = resolvedSequenceNumber,
            InvokeTimeout = invokeTimeout,
            Resequenced = resequenced
        });
    }

    public StateItemAdded AddStateItem(string name, StateItemType type)
    {
        if (_registrationStateItems.Any(item => item.Name == name))
        {
            throw new InvalidOperationException($"State item with name '{name}' already exists.");
        }

        return On(new StateItemAdded { Name = name, Type = type });
    }

    public IEnumerable<MessageDefinition> GetMessages()
    {
        return _messageDefinitions.AsReadOnly();
    }

    public IEnumerable<StateItemDefinition> GetStateItems()
    {
        return _registrationStateItems.AsReadOnly();
    }

    private Registered On(Registered registered)
    {
        Name = registered.Name;
        Description = registered.Description;

        return registered;
    }

    private StateItemAdded On(StateItemAdded stateItemAdded)
    {
        _registrationStateItems.Add(new(stateItemAdded.Name, stateItemAdded.Type));

        return stateItemAdded;
    }

    private MessageAdded On(MessageAdded messageAdded)
    {
        foreach (var resequencedMessage in messageAdded.Resequenced)
        {
            _messageDefinitions
                .First(messageDefinition => messageDefinition.TypeName == resequencedMessage.TypeName)
                .SequenceNumber = resequencedMessage.SequenceNumber;
        }

        _messageDefinitions.Add(new(messageAdded.TypeName, messageAdded.SequenceNumber, messageAdded.InvokeTimeout));

        return messageAdded;
    }

    public Registered Register(string name, string description)
    {
        return On(new Registered
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name)),
            Description = !string.IsNullOrWhiteSpace(description) ? description : throw new ArgumentNullException(nameof(description))
        });
    }

    public class MessageDefinition(string typeName, int sequenceNumber, TimeSpan? invokeTimeout = null)
    {
        public TimeSpan? InvokeTimeout { get; } = invokeTimeout;
        public int SequenceNumber { get; internal set; } = sequenceNumber;

        public string TypeName { get; } = !string.IsNullOrWhiteSpace(typeName) ? typeName : throw new ArgumentNullException(nameof(typeName));
    }

    public class StateItemDefinition(string name, StateItemType type)
    {
        public string Name { get; } = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
        public StateItemType Type { get; } = type;

        public bool TryParse(string s, out object? result)
        {
            switch (Type)
            {
                case StateItemType.String:
                {
                    result = s;
                    return true;
                }
                case StateItemType.DateTime:
                {
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dateTimeResult))
                    {
                        result = dateTimeResult;
                        return true;
                    }

                    break;
                }
                case StateItemType.Decimal:
                {
                    if (decimal.TryParse(s, out var decimalResult))
                    {
                        result = decimalResult;
                        return true;
                    }

                    break;
                }
                case StateItemType.Boolean:
                {
                    if (bool.TryParse(s, out var boolResult))
                    {
                        result = boolResult;
                        return true;
                    }

                    break;
                }
                case StateItemType.Guid:
                {
                    if (Guid.TryParse(s, out var guidResult))
                    {
                        result = guidResult;
                        return true;
                    }

                    break;
                }
            }

            result = null;

            return false;
        }
    }
}