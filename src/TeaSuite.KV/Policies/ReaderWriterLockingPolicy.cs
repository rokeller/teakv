using System;
using System.Threading;

namespace TeaSuite.KV.Policies;

/// <summary>
/// Implements the <see cref="ILockingPolicy"/> allowing concurrent readers but
/// only exclusive access for writing.
/// </summary>
public sealed class ReaderWriterLockingPolicy : ILockingPolicy
{
    private readonly ReaderWriterLockSlim rwlock = new(LockRecursionPolicy.NoRecursion);

    /// <inheritdoc/>
    public IDisposable? AcquireReadLock()
    {
        rwlock.EnterReadLock();
        return new ReadLockCompletion(rwlock);
    }

    /// <inheritdoc/>
    public IDisposable? AcquireWriteLock()
    {
        rwlock.EnterWriteLock();
        return new WriteLockCompletion(rwlock);
    }

    /// <summary>
    /// A simple struct implementing <see cref="IDisposable"/> to release the
    /// read lock when disposed.
    /// </summary>
    private readonly record struct ReadLockCompletion(
        ReaderWriterLockSlim Lock
        ) : IDisposable
    {
        public void Dispose()
        {
            Lock.ExitReadLock();
        }
    }

    /// <summary>
    /// A simple struct implementing <see cref="IDisposable"/> to release the
    /// read lock when disposed.
    /// </summary>
    private readonly record struct WriteLockCompletion(
        ReaderWriterLockSlim Lock
        ) : IDisposable
    {
        public void Dispose()
        {
            Lock.ExitWriteLock();
        }
    }
}
