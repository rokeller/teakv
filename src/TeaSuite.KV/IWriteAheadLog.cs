using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeaSuite.KV;

/// <summary>
/// Defines the contract for a write-ahead log for write operations (set and
/// delete) to a key-value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the key-value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the key-value store.
/// </typeparam>
public interface IWriteAheadLog<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Starts the WAL.
    /// </summary>
    /// <param name="recover">
    /// An <see cref="Action{T}"/> that is called to receive an
    /// <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// values that are recovered from previous write-ahead logs, if any. These
    /// key-value store entries should typically be added to the in-memory
    /// store on startup for recovery.
    /// </param>
    /// <remarks>
    /// This method must only be called after any recovery has been executed if
    /// needed.
    /// </remarks>
    void Start(Action<IEnumerator<StoreEntry<TKey, TValue>>>? recover);

    /// <summary>
    /// Asynchronously announces that the given <paramref name="entry"/> should
    /// be written.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to be written.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <see cref="bool"/> indicating
    /// whether the announced write operation was successfully written to the
    /// write-ahead log.
    /// </returns>
    ValueTask<bool> AnnounceWriteAsync(StoreEntry<TKey, TValue> entry);

    /// <summary>
    /// Asynchronously prepares for the transition of one write-ahead log to
    /// the next, as needed when swapping an in-memory key-value store for
    /// another.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IDisposable"/> that finishes the prepare
    /// transition operation when disposed.
    /// </returns>
    ValueTask<IDisposable> PrepareTransitionAsync();

    /// <summary>
    /// Asynchronously completes the transition of one write-ahead log to the
    /// next, as needed after the old in-memory store has successfully been
    /// committed to a segment, thus indicating that the old write-ahead log
    /// that was used to track write operations for the old in-memory store is
    /// no longer needed.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IDisposable"/> that finishes the complete
    /// transition operation when disposed.
    /// </returns>
    ValueTask<IDisposable> CompleteTransitionAsync();

    /// <summary>
    /// Shuts the write-ahead log down, indicating a clean state and thus no
    /// recovery needed before the next start.
    /// </summary>
    void Shutdown();
}
