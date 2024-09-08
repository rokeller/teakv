using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TeaSuite.KV;

partial class DefaultKeyValueStore<TKey, TValue>
{
    /// <inheritdoc/>
    protected override IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> GetEntriesEnumerators(
        Range<TKey> range)
    {
        // We need to acquire the read-lock before enumeration starts, so let's
        // do that here.
        IDisposable? readLock = lockingPolicy.AcquireReadLock();
        IEnumerator<StoreEntry<TKey, TValue>> inMemEnumerator =
            CreateInMemoryEnumerator(range, readLock);
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> segmentEnumerators =
            base.GetEntriesEnumerators(range);

        // The enumerator for the in-memory entries must always be first in the
        // list so as to make sure its entries take precendence over entries
        // from persisted segments.
        return segmentEnumerators.Prepend(inMemEnumerator);
    }

    /// <inheritdoc/>
    protected override IEnumerator<StoreEntry<TKey, TValue>> CreateEntriesEnumerator(
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> enumerators)
    {
        IEnumerator<StoreEntry<TKey, TValue>> first = enumerators.First();

        // If the first enumerator of all passed enumerators is our own
        // InMemoryEnumerator, we need to extract the read locker from it so we
        // can release it (through the GuardingEnumerator) once the enumerator
        // is disposed.
        if (first is InMemoryEnumerator inMemoryEnumerator)
        {
            IDisposable? readLock = inMemoryEnumerator.ReadLock;
            if (null != readLock)
            {
                return new GuardingEnumerator<StoreEntry<TKey, TValue>>(
                    readLock, base.CreateEntriesEnumerator(enumerators));
            }
        }

        return base.CreateEntriesEnumerator(enumerators);
    }

    /// <summary>
    /// Create an <see cref="InMemoryEnumerator"/> that enumerates the entries
    /// from the in-memory store or stores in case there is an 'old' (being
    /// written to disk) and a new one.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> to enumerate.
    /// </param>
    /// <param name="readLock">
    /// The <see cref="IDisposable"/> that represents the read lock or null if
    /// a read lock is not used.
    /// </param>
    /// <returns>
    /// An <see cref="InMemoryEnumerator"/> value that represents an
    /// <see cref="IEnumerator{T}"/> to enumerate the entries from the in-memory
    /// store(s).
    /// </returns>
    private InMemoryEnumerator CreateInMemoryEnumerator(
        Range<TKey> range,
        IDisposable? readLock)
    {
        IEnumerator<StoreEntry<TKey, TValue>> curEnumerator =
            memoryStores.Current.GetOrderedEnumerator(range);
        IEnumerator<StoreEntry<TKey, TValue>>? oldEnumerator =
            memoryStores.Old?.GetOrderedEnumerator(range);

        if (null != oldEnumerator)
        {
            return new(
                readLock,
                new MergingEnumerator<StoreEntry<TKey, TValue>>(
                    curEnumerator, oldEnumerator));
        }
        else
        {
            return new(readLock, curEnumerator);
        }
    }

    private readonly record struct InMemoryEnumerator :
        IEnumerator<StoreEntry<TKey, TValue>>
    {
        private readonly IEnumerator<StoreEntry<TKey, TValue>> inner;

        public InMemoryEnumerator(
            IDisposable? readLock,
            IEnumerator<StoreEntry<TKey, TValue>> inner)
        {
            ReadLock = readLock;
            this.inner = inner;
        }

        public IDisposable? ReadLock { get; }

        public StoreEntry<TKey, TValue> Current => inner.Current;

        object IEnumerator.Current => ((IEnumerator)inner).Current;

        public void Dispose()
        {
            inner.Dispose();
        }

        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }
    }
}
