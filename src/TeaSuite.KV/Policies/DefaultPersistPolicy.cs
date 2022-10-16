using System;

namespace TeaSuite.KV.Policies;

/// <summary>
/// A default implementation of the <see cref="IPersistPolicy"/>.
/// </summary>
public readonly struct DefaultPersistPolicy : IPersistPolicy
{
    private readonly long minEntryCount;
    private readonly TimeSpan maxMemoryAge;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultPersistPolicy"/>. This would persist in-memory Key/Value stores
    /// with more than 100K entries, or after at most 1 hour.
    /// </summary>
    public DefaultPersistPolicy() : this(100_000, TimeSpan.FromHours(1)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultPersistPolicy"/> using the given parameters.
    /// </summary>
    /// <param name="minEntryCount">
    /// A <see cref="long"/> value specifying the minimum number of entries in the store before persisting.
    /// </param>
    /// <param name="maxMemoryAge">
    /// A <see cref="TimeSpan"/> value specyfing the maximum age of the in-memory store.
    /// </param>
    public DefaultPersistPolicy(long minEntryCount, TimeSpan maxMemoryAge)
    {
        this.minEntryCount = minEntryCount;
        this.maxMemoryAge = maxMemoryAge;
    }

    /// <inheritdoc/>
    public bool ShouldPersist(long entryCount, TimeSpan timeSinceLastPersist)
    {
        return entryCount >= minEntryCount || timeSinceLastPersist >= maxMemoryAge;
    }
}
