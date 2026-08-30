namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class ProcessDefinition
{
    public string Description { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public List<Message> Messages { get; set; } = [];
    public string Name { get; set; } = string.Empty;

    public List<StateItem> StateItems { get; set; } = [];

    public class Message
    {
        public int SequenceNumber { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    public class Specification
    {
        public int MaximumRows { get; set; }
        public string? Name { get; set; }
    }

    public class StateItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}