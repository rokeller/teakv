using System;

namespace TeaSuite.KV.Policies;

/// <summary>
/// Defines the contract for a policy that assists with read/write locking.
/// </summary>
public interface ILockingPolicy
{
    /// <summary>
    /// Blocks until the read lobk is acquired, if any.
    /// </summary>
    /// <returns>
    /// An <see cref="IDisposable"/> that can be used to release the read lock
    /// or null if there's no lock.
    /// </returns>
    IDisposable? AcquireReadLock();

    /// <summary>
    /// Blocks until the write lock is acquired, if any.
    /// </summary>
    /// <returns>
    /// An <see cref="IDisposable"/> that can be used to release the write lock
    /// or null if there's no lock.
    /// </returns>
    IDisposable? AcquireWriteLock();
}
