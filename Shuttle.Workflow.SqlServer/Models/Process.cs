using System.ComponentModel.DataAnnotations;

namespace Shuttle.Workflow.SqlServer.Models;

public class Process

{
    public ICollection<ProcessCommit> Commits { get; set; } = new List<ProcessCommit>();
    public Guid? ContinuationMessageId { get; set; }
    public DateTimeOffset? ContinuationRegisteredAt { get; set; }
    public Guid? ContinuationToken { get; set; }
    public DateTimeOffset? DateCompleted { get; set; }

    public DateTimeOffset DateRegistered { get; set; }
    public DateTimeOffset? DeferredTill { get; set; }

    [StringLength(512)]
    public string? Description { get; set; }

    [Key]
    public Guid Id { get; set; }

    [StringLength(512)]
    public string? Key { get; set; }

    public ICollection<ProcessMessage> Messages { get; set; } = new List<ProcessMessage>();

    [Required]
    [StringLength(512)]
    public string Name { get; set; } = null!;

    public DateTimeOffset? OverdueAt { get; set; }

    [StringLength(130)]
    public string Status { get; set; } = "Registered";

    [StringLength(512)]
    public string? StatusMessage { get; set; }

    public class Specification : Specification<Specification>
    {
        private readonly List<string> _excludedStatuses = [];
        private readonly List<string> _includedStatuses = [];
        public IEnumerable<string> ExcludedStatuses => _excludedStatuses.AsReadOnly();
        public DateTimeOffset? FromDateCompletedInclusive { get; private set; }

        public DateTimeOffset? FromDateRegisteredInclusive { get; private set; }
        public bool HasExcludedStatuses => _excludedStatuses.Any();

        public bool HasIncludedStatuses => _includedStatuses.Any();

        public IEnumerable<string> IncludedStatuses => _includedStatuses.AsReadOnly();
        public string? Key { get; private set; }


        public string? KeyMatch { get; private set; }

        public string? Name { get; private set; }

        public string? NameMatch { get; private set; }
        public bool ShouldIncludeCommits { get; private set; }
        public bool ShouldIncludeMessages { get; private set; }
        public DateTimeOffset? ToDateCompletedExclusive { get; private set; }
        public DateTimeOffset? ToDateRegisteredExclusive { get; private set; }

        public Specification ActiveOnly()
        {
            ExcludedStatus("Completed");
            ExcludedStatus("Abandoned");

            return this;
        }

        public Specification ExcludedStatus(string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status);

            if (!_excludedStatuses.Contains(status))
            {
                _excludedStatuses.Add(status);
            }

            return this;
        }

        public Specification IncludeCommits()
        {
            ShouldIncludeCommits = true;

            return this;
        }

        public Specification IncludedStatus(string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status);

            if (!_includedStatuses.Contains(status))
            {
                _includedStatuses.Add(status);
            }

            return this;
        }

        public Specification IncludeMessages()
        {
            ShouldIncludeMessages = true;

            return this;
        }

        public Specification WithFromDateCompletedInclusive(DateTimeOffset fromDateCompletedInclusive)
        {
            if (ToDateCompletedExclusive.HasValue && fromDateCompletedInclusive >= ToDateCompletedExclusive.Value)
            {
                throw new ApplicationException($"`FromDateCompletedInclusive` ({fromDateCompletedInclusive:O}) must be before `ToDateCompletedExclusive` ({ToDateCompletedExclusive.Value:O}).");
            }

            FromDateCompletedInclusive = fromDateCompletedInclusive;
            return this;
        }

        public Specification WithFromDateRegisteredInclusive(DateTimeOffset fromDateRegisteredInclusive)
        {
            if (ToDateRegisteredExclusive.HasValue && fromDateRegisteredInclusive >= ToDateRegisteredExclusive.Value)
            {
                throw new ApplicationException($"`FromDateRegisteredInclusive` ({fromDateRegisteredInclusive:O}) must be before `ToDateRegisteredExclusive` ({ToDateRegisteredExclusive.Value:O}).");
            }

            FromDateRegisteredInclusive = fromDateRegisteredInclusive;
            return this;
        }

        public Specification WithKey(string key)
        {
            Key = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentNullException(nameof(key));

            return WithMaximumRows(1);
        }

        public Specification WithKeyMatch(string match)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(match);

            KeyMatch = match;

            return this;
        }

        public Specification WithName(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
            return this;
        }

        public Specification WithNameMatch(string match)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(match);

            NameMatch = match;

            return this;
        }

        public Specification WithToDateCompletedExclusive(DateTimeOffset toDateCompletedExclusive)
        {
            if (FromDateCompletedInclusive.HasValue && FromDateCompletedInclusive.Value >= toDateCompletedExclusive)
            {
                throw new ApplicationException($"`ToDateCompletedExclusive` ({toDateCompletedExclusive:O}) must be after `FromDateCompletedInclusive` ({FromDateCompletedInclusive.Value:O}).");
            }

            ToDateCompletedExclusive = toDateCompletedExclusive;
            return this;
        }

        public Specification WithToDateRegisteredExclusive(DateTimeOffset toDateRegisteredExclusive)
        {
            if (FromDateRegisteredInclusive.HasValue && FromDateRegisteredInclusive.Value >= toDateRegisteredExclusive)
            {
                throw new ApplicationException($"`ToDateRegisteredExclusive` ({toDateRegisteredExclusive:O}) must be after `FromDateRegisteredInclusive` ({FromDateRegisteredInclusive.Value:O}).");
            }

            ToDateRegisteredExclusive = toDateRegisteredExclusive;
            return this;
        }
    }
}