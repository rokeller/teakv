using System;

namespace TeaSuite.KV.Policies;

/// <summary>
/// Implements the <see cref="ILockingPolicy"/> doing no locking.
/// </summary>
public sealed class NullLockingPolicy : ILockingPolicy
{
    /// <summary>
    /// Gets a default instance of <see cref="NullLockingPolicy"/>.
    /// </summary>
    public static readonly NullLockingPolicy Instance = new();

    /// <inheritdoc/>
    public IDisposable? AcquireReadLock()
    {
        return null;
    }

    /// <inheritdoc/>
    public IDisposable? AcquireWriteLock()
    {
        return null;
    }
}
