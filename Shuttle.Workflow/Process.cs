using Shuttle.Contract;
using Shuttle.Workflow.Events.Process.v1;

namespace Shuttle.Workflow;

public class Process
{
    private readonly HashSet<string> _commits = [];

    private readonly List<Message> _messages = [];
    public Guid? ContinuationMessageId { get; private set; }
    public DateTimeOffset? ContinuationRegisteredAt { get; private set; }
    public Guid? ContinuationToken { get; private set; }
    public DateTimeOffset? DateCompleted { get; private set; }
    public DateTimeOffset DateRegistered { get; private set; }
    public DateTimeOffset? DeferredTill { get; private set; }
    public string? Description { get; private set; }

    public bool HasCompleted => DateCompleted.HasValue;
    public bool HasNextMessage => GetNextMessage() != null;
    public string? Key { get; private set; }
    public int MessageCount => _messages.Count;

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset? OverdueAt { get; private set; }
    public string Status { get; private set; } = StatusNames.Registered;
    public string? StatusMessage { get; private set; }

    public Abandoned Abandon(string message)
    {
        if (HasCompleted)
        {
            throw new InvalidOperationException("Process has already completed.");
        }

        return On(new Abandoned { Message = message, DateCompleted = DateTimeOffset.UtcNow });
    }

    public MessageAdded AddMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var sequenceNumber = _messages.Count + 1;

        if (!message.SequenceNumber.Equals(sequenceNumber))
        {
            throw new InvalidOperationException($"Message sequence number '{message.SequenceNumber}' is not valid as it should be '{sequenceNumber}'.");
        }

        return On(new MessageAdded
        {
            MessageId = message.Id,
            TypeName = message.TypeName,
            SequenceNumber = message.SequenceNumber,
            InvokeTimeout = message.InvokeTimeout
        });
    }

    public Committed? Commit(string key, DateTimeOffset dateCommitted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _commits.Contains(key) ? null : On(new Committed { Key = key, DateCommitted = dateCommitted });
    }

    public Completed Complete()
    {
        if (HasNextMessage)
        {
            throw new InvalidOperationException("Process can not be completed as there are outstanding messages.");
        }

        return On(new Completed { DateCompleted = DateTimeOffset.UtcNow });
    }

    public MessageCompleted CompleteMessage(Guid id)
    {
        GetMessage(id);

        return On(new MessageCompleted { MessageId = id, DateCompleted = DateTimeOffset.UtcNow });
    }

    public MessageProgressSet SetMessageProgress(Guid id, int? itemsTotal, int itemsCompleted)
    {
        GetMessage(id);

        Guard.Against<ArgumentException>(itemsCompleted < 0, "'itemsCompleted' may not be less than zero.");
        Guard.Against<ArgumentException>(itemsTotal.HasValue && itemsCompleted > itemsTotal.Value, "'itemsCompleted' may not be greater than 'itemsTotal'.");

        return On(new MessageProgressSet { MessageId = id, ItemsTotal = itemsTotal, ItemsCompleted = itemsCompleted });
    }

    public Continued Continue()
    {
        if (HasCompleted)
        {
            throw new InvalidOperationException("Process has already completed.");
        }

        return On(new Continued { StatusMessage = $"[continue]:{GetNextMessage()?.Id ?? Guid.Empty}" });
    }

    public Deferred? Defer(DateTimeOffset deferredTill)
    {
        return deferredTill > DateTimeOffset.UtcNow
            ? On(new Deferred { DeferredTill = deferredTill.ToUniversalTime() })
            : null;
    }

    public Failed Fail(string message)
    {
        if (HasCompleted)
        {
            throw new InvalidOperationException("Process has already completed.");
        }

        return On(new Failed { Message = message, DateCompleted = DateTimeOffset.UtcNow });
    }

    public DateTimeOffset? GetEffectiveOverdueAt()
    {
        var current = GetNextMessage();
        var messageOverdueAt = current is { DateSent: not null, InvokeTimeout: not null }
            ? current.DateSent!.Value + current.InvokeTimeout!.Value
            : (DateTimeOffset?)null;

        if (OverdueAt.HasValue && messageOverdueAt.HasValue)
        {
            return OverdueAt.Value < messageOverdueAt.Value ? OverdueAt.Value : messageOverdueAt.Value;
        }

        return OverdueAt ?? messageOverdueAt;
    }

    public Message GetMessage(Guid id)
    {
        return _messages.FirstOrDefault(message => message.Id.Equals(id)) ?? throw new ArgumentException($"Message with Id '{id}' not found.");
    }

    public IEnumerable<Message> GetMessages()
    {
        return _messages.AsReadOnly();
    }

    public Message? GetNextMessage()
    {
        return _messages.FirstOrDefault(item => !item.HasCompleted);
    }

    public bool HasStatus(string status)
    {
        return Status.Equals(status, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDeferred()
    {
        return HasStatus(StatusNames.Deferred) && DeferredTill.HasValue && DeferredTill.Value > DateTimeOffset.UtcNow;
    }

    public bool IsOverdue()
    {
        return GetEffectiveOverdueAt() is { } overdueAt && overdueAt <= DateTimeOffset.UtcNow;
    }

    private Registered On(Registered registered)
    {
        Name = registered.Name;
        Key = registered.Key;
        Description = registered.Description;
        DateRegistered = registered.DateRegistered;
        Status = StatusNames.Registered;

        return registered;
    }

    private Deferred On(Deferred deferred)
    {
        DeferredTill = deferred.DeferredTill;
        Status = StatusNames.Deferred;

        return deferred;
    }

    private OverdueAtSet On(OverdueAtSet overdueAtSet)
    {
        OverdueAt = overdueAtSet.OverdueAt;

        return overdueAtSet;
    }

    private Completed On(Completed completed)
    {
        Status = StatusNames.Completed;
        DateCompleted = completed.DateCompleted;

        return completed;
    }

    private Waited On(Waited waited)
    {
        Status = StatusNames.Waiting;
        StatusMessage = waited.Message;

        return waited;
    }

    private Failed On(Failed failed)
    {
        Status = StatusNames.Failed;
        StatusMessage = failed.Message;
        DateCompleted = failed.DateCompleted;

        return failed;
    }

    private Abandoned On(Abandoned abandoned)
    {
        Status = StatusNames.Abandoned;
        StatusMessage = abandoned.Message;
        DateCompleted = abandoned.DateCompleted;

        return abandoned;
    }

    private Started On(Started started)
    {
        Status = StatusNames.Started;
        StatusMessage = null;

        return started;
    }

    private MessageAdded On(MessageAdded messageAdded)
    {
        _messages.Add(new(messageAdded.MessageId, messageAdded.TypeName, messageAdded.SequenceNumber, messageAdded.InvokeTimeout));

        return messageAdded;
    }

    private MessageCompleted On(MessageCompleted messageCompleted)
    {
        GetMessage(messageCompleted.MessageId).Completed(messageCompleted.DateCompleted);

        ContinuationToken = null;
        ContinuationMessageId = null;
        ContinuationRegisteredAt = null;

        return messageCompleted;
    }

    private MessageProgressSet On(MessageProgressSet messageProgressSet)
    {
        GetMessage(messageProgressSet.MessageId).SetProgress(messageProgressSet.ItemsTotal, messageProgressSet.ItemsCompleted);

        return messageProgressSet;
    }

    private MessageSent On(MessageSent messageSent)
    {
        GetMessage(messageSent.MessageId).Sent(messageSent.DateSent);

        return messageSent;
    }

    private Continued On(Continued continued)
    {
        DeferredTill = null;
        Status = StatusNames.Started;
        StatusMessage = continued.StatusMessage;
        ContinuationToken = null;
        ContinuationMessageId = null;
        ContinuationRegisteredAt = null;

        return continued;
    }

    private ContinuationSet On(ContinuationSet continuationSet)
    {
        ContinuationToken = continuationSet.Token;
        ContinuationMessageId = continuationSet.MessageId;
        ContinuationRegisteredAt = continuationSet.RegisteredAt;

        return continuationSet;
    }

    private Committed On(Committed committed)
    {
        _commits.Add(committed.Key);

        return committed;
    }

    public Registered Register(string name, string? key, string? description)
    {
        return On(new Registered
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentNullException(nameof(name)) : name,
            Key = key,
            Description = description,
            DateRegistered = DateTimeOffset.UtcNow
        });
    }

    public MessageSent SendMessage(Guid id)
    {
        GetMessage(id);

        return On(new MessageSent { MessageId = id, DateSent = DateTimeOffset.UtcNow });
    }

    public Started Start()
    {
        return On(new Started());
    }

    public Waited Wait(string message)
    {
        return On(new Waited { Message = message });
    }

    public ContinuationSet WithContinuation(DateTimeOffset? registeredAt = null)
    {
        if (HasCompleted || !HasNextMessage)
        {
            throw new InvalidOperationException("Process has already completed.");
        }

        return On(new ContinuationSet
        {
            Token = Guid.NewGuid(),
            MessageId = GetNextMessage()!.Id,
            RegisteredAt = registeredAt ?? DateTimeOffset.UtcNow
        });
    }

    public OverdueAtSet WithOverdueAt(DateTimeOffset overdueAt)
    {
        return On(new OverdueAtSet { OverdueAt = overdueAt.ToUniversalTime() });
    }

    public class Message(Guid id, string typeName, int sequenceNumber, TimeSpan? invokeTimeout = null)
    {
        public DateTimeOffset? DateCompleted { get; private set; }
        public DateTimeOffset? DateSent { get; private set; }
        public bool HasCompleted => DateCompleted.HasValue;
        public Guid Id { get; } = id;
        public TimeSpan? InvokeTimeout { get; } = invokeTimeout;
        public int ItemsCompleted { get; private set; }
        public int? ItemsTotal { get; private set; }
        public int SequenceNumber { get; } = sequenceNumber;
        public string TypeName { get; } = !string.IsNullOrWhiteSpace(typeName) ? typeName : throw new ArgumentNullException(nameof(typeName));

        internal void Completed(DateTimeOffset dateCompleted)
        {
            DateCompleted = dateCompleted;
        }

        internal void Sent(DateTimeOffset dateSent)
        {
            DateSent = dateSent;
        }

        internal void SetProgress(int? itemsTotal, int itemsCompleted)
        {
            ItemsTotal = itemsTotal;
            ItemsCompleted = itemsCompleted;
        }
    }

    public static class StatusNames
    {
        public const string Registered = "Registered";
        public const string Deferred = "Deferred";
        public const string Started = "Started";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Abandoned = "Abandoned";
        public const string Waiting = "Waiting";
    }
}