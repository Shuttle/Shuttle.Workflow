namespace Shuttle.Workflow.Events.ProcessDefinition.v1;

public class MessageAdded
{
    public TimeSpan? InvokeTimeout { get; set; }

    /// <summary>
    ///     Existing message definitions whose sequence number is bumped by one as a mechanical consequence of
    ///     this message being inserted ahead of them.
    /// </summary>
    public List<ResequencedMessage> Resequenced { get; set; } = [];

    public int SequenceNumber { get; set; }
    public string TypeName { get; set; } = string.Empty;

    public class ResequencedMessage
    {
        public int SequenceNumber { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }
}