using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeaSuite.KV;

/// <summary>
/// Implements the <see cref="IWriteAheadLog{TKey, TValue}"/> interface to
/// create a null write-ahead log for key-value write operations. With this
/// implementation, no WAL is kept and recovery is never possible.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the key-value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the key-value store.
/// </typeparam>
public sealed class NullWriteAheadLog<TKey, TValue> : IWriteAheadLog<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Gets a default instance of <see cref="NullWriteAheadLog{TKey, TValue}"/>.
    /// </summary>
    public static readonly NullWriteAheadLog<TKey, TValue> Instance = new();

    /// <inheritdoc/>
    public void Start(Action<IEnumerator<StoreEntry<TKey, TValue>>>? recover)
    {
        // Intentionally left blank.
    }

    /// <inheritdoc/>
    public ValueTask<bool> AnnounceWriteAsync(StoreEntry<TKey, TValue> entry)
    {
        // All write operations are always successful.
        return new(true);
    }

    /// <inheritdoc/>
    public ValueTask<IDisposable> PrepareTransitionAsync()
    {
        // Preparing a WAL transition is always a no-op.
        return new(NullDisposable.Instance);
    }

    /// <inheritdoc/>
    public ValueTask<IDisposable> CompleteTransitionAsync()
    {
        // Completing a WAL transition is always a no-op.
        return new(NullDisposable.Instance);
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        // Intentionally left blank.
    }
}
