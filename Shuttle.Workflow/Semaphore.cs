using Shuttle.Contract;

namespace Shuttle.Workflow;

public class Semaphore(string key, string owner, DateTimeOffset dateRegistered, DateTimeOffset? expiresAt)
{
    public DateTimeOffset DateRegistered { get; private set; } = dateRegistered;
    public DateTimeOffset? ExpiresAt { get; private set; } = expiresAt;
    public string Key { get; } = !string.IsNullOrWhiteSpace(key) ? key : throw new ArgumentNullException(nameof(key));
    public string Owner { get; private set; } = !string.IsNullOrWhiteSpace(owner) ? owner : throw new ArgumentNullException(nameof(owner));

    public Semaphore ExpireAt(DateTimeOffset when)
    {
        ExpiresAt = when;
        return this;
    }

    public bool HasExpired(DateTimeOffset now)
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= now;
    }

    public Semaphore Reassign(string owner, DateTimeOffset dateRegistered)
    {
        Owner = Guard.AgainstEmpty(owner);
        DateRegistered = dateRegistered;
        return this;
    }

    public bool SatisfiesMinimumLifetime(DateTimeOffset now, TimeSpan minimumLifetime)
    {
        return !ExpiresAt.HasValue || ExpiresAt.Value > now + minimumLifetime;
    }
}