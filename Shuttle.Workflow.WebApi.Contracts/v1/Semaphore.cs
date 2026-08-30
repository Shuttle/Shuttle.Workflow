namespace Shuttle.Workflow.WebApi.Contracts.v1;

public class Semaphore
{
    public DateTimeOffset DateRegistered { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;

    public class Specification
    {
        public DateTimeOffset? FromExpiresAtInclusive { get; set; }
        public string? Key { get; set; }
        public string? KeyMatch { get; set; }
        public int MaximumRows { get; set; }
        public string? Owner { get; set; }
        public string? OwnerMatch { get; set; }
        public DateTimeOffset? ToExpiresAtExclusive { get; set; }
    }
}