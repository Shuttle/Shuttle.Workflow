namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class Process
{
    public List<Commit> Commits { get; set; } = [];
    public Guid? ContinuationMessageId { get; set; }
    public DateTimeOffset? ContinuationRegisteredAt { get; set; }
    public Guid? ContinuationToken { get; set; }
    public DateTimeOffset? DateCompleted { get; set; }
    public DateTimeOffset DateRegistered { get; set; }
    public DateTimeOffset? DeferredTill { get; set; }
    public string? Description { get; set; }
    public Guid Id { get; set; }
    public string? Key { get; set; }

    public List<Message> Messages { get; set; } = [];
    public string Name { get; set; } = null!;
    public DateTimeOffset? OverdueAt { get; set; }

    public State State { get; set; } = new();
    public string Status { get; set; } = null!;
    public string? StatusMessage { get; set; }

    public class Commit
    {
        public DateTimeOffset DateCommitted { get; set; }
        public string Key { get; set; } = string.Empty;
    }

    public class Message
    {
        public DateTimeOffset? DateCompleted { get; set; }
        public DateTimeOffset? DateSent { get; set; }
        public Guid Id { get; set; }
        public Guid ProcessId { get; set; }
        public int SequenceNumber { get; set; }
        public string TypeName { get; set; } = null!;
    }

    public class Specification
    {
        public bool? ActiveOnly { get; set; }
        public List<string> ExcludedStatuses { get; set; } = [];
        public DateTimeOffset? FromDateCompletedInclusive { get; set; }
        public DateTimeOffset? FromDateRegisteredInclusive { get; set; }
        public List<Guid> Ids { get; set; } = [];
        public List<string> IncludedStatuses { get; set; } = [];
        public string? Key { get; set; }
        public string? KeyMatch { get; set; }
        public int MaximumRows { get; set; }
        public string? Name { get; set; }
        public string? NameMatch { get; set; }
        public bool ShouldIncludeCommits { get; set; }
        public bool ShouldIncludeMessages { get; set; }
        public DateTimeOffset? ToDateCompletedExclusive { get; set; }
        public DateTimeOffset? ToDateRegisteredExclusive { get; set; }
    }
}