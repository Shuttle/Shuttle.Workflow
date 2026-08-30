using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[Index(nameof(Key), IsUnique = true, Name = $"IX_{nameof(Semaphore)}")]
public class Semaphore
{
    public DateTimeOffset DateRegistered { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    [Key]
    public Guid Id { get; set; }

    [StringLength(512)]
    public string Key { get; set; } = string.Empty;

    [StringLength(512)]
    public string Owner { get; set; } = string.Empty;

    public class Specification
    {
        public DateTimeOffset? FromExpiresAtInclusive { get; private set; }
        public string? Key { get; private set; }
        public string? KeyMatch { get; private set; }
        public int MaximumRows { get; private set; }
        public string? Owner { get; private set; }
        public string? OwnerMatch { get; private set; }
        public DateTimeOffset? ToExpiresAtExclusive { get; private set; }

        public Specification WithFromExpiresAtInclusive(DateTimeOffset fromExpiresAtInclusive)
        {
            if (ToExpiresAtExclusive.HasValue && fromExpiresAtInclusive >= ToExpiresAtExclusive.Value)
            {
                throw new ApplicationException($"`{nameof(fromExpiresAtInclusive)}` ({fromExpiresAtInclusive:O}) must be before `{nameof(ToExpiresAtExclusive)}` ({ToExpiresAtExclusive.Value:O}).");
            }

            FromExpiresAtInclusive = fromExpiresAtInclusive;
            return this;
        }

        public Specification WithKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            Key = key;
            return this;
        }

        public Specification WithKeyMatch(string keyMatch)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyMatch);
            KeyMatch = keyMatch;
            return this;
        }

        public Specification WithMaximumRows(int maximumRows)
        {
            MaximumRows = maximumRows;
            return this;
        }

        public Specification WithOwner(string owner)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);
            Owner = owner;
            return this;
        }

        public Specification WithOwnerMatch(string ownerMatch)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ownerMatch);
            OwnerMatch = ownerMatch;
            return this;
        }

        public Specification WithToExpiresAtExclusive(DateTimeOffset toExpiresAtExclusive)
        {
            if (FromExpiresAtInclusive.HasValue && FromExpiresAtInclusive.Value >= toExpiresAtExclusive)
            {
                throw new ApplicationException($"`{nameof(toExpiresAtExclusive)}` ({toExpiresAtExclusive:O}) must be after `{nameof(FromExpiresAtInclusive)}` ({FromExpiresAtInclusive.Value:O}).");
            }

            ToExpiresAtExclusive = toExpiresAtExclusive;
            return this;
        }
    }
}