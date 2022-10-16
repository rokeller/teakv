using System;

namespace TeaSuite.KV.Policies;

/// <summary>
/// Defines the contract for a policy that helps determine when the in-memory Key/Value store should be persisted to
/// segments
/// </summary>
public interface IPersistPolicy
{
    /// <summary>
    /// Determines if the in-memory Key/Value store should be persisted to segments.
    /// </summary>
    /// <param name="entryCount">
    /// A <see cref="long"/> value that specifies how many entries are currently held in-memory.
    /// </param>
    /// <param name="timeSinceLastPersist">
    /// A <see cref="TimeSpan"/> value that specifies how much time has passed since the last time the in-memory store
    /// was persisted.
    /// </param>
    /// <returns>
    /// True if the in-memory Key/Value store should be persisted, or false otherwise.
    /// </returns>
    bool ShouldPersist(long entryCount, TimeSpan timeSinceLastPersist);
}
