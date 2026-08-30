namespace Shuttle.Workflow.Messages.v1;

public class RegisterProcess
{
    public DateTimeOffset? DeferredTill { get; set; }
    public string? Description { get; set; }
    public Guid Id { get; set; }
    public string? Key { get; set; }
    public List<MessageDefinition> Messages { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? OverdueAt { get; set; }
    public Guid StateId { get; set; }
    public List<StateItemValue> StateItems { get; set; } = [];

    public class MessageDefinition
    {
        public TimeSpan? InvokeTimeout { get; set; }
        public int SequenceNumber { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }
}