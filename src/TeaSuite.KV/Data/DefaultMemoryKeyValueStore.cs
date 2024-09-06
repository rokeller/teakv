using System;
using System.Collections.Generic;
using System.Threading;

namespace TeaSuite.KV.Data;

/// <summary>
/// Implements <see cref="IMemoryKeyValueStore{TKey, TValue}"/> using an AVL-tree.
/// </summary>
/// <typeparam name="TKey">
/// Type type of the keys used for entries of the store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries of the store.
/// </typeparam>
public sealed class DefaultMemoryKeyValueStore<TKey, TValue> : IMemoryKeyValueStore<TKey, TValue>
    where TKey : IComparable<TKey>
{
    #region Consts

    /// <summary>
    /// The state indicating that the tree can be written to.
    /// </summary>
    private const long StateReadWrite = 0;

    /// <summary>
    /// The state indicating that the tree is read-only and can no longer be updated.
    /// </summary>
    private const long StateReadOnly = 1;

    #endregion

    #region Fields

    private readonly AvlTree<StoreEntry<TKey, TValue>> tree =
        new(StoreEntry<TKey, TValue>.KeyComparer);

    /// <summary>
    /// Holds a count of ongoing read-only operations (like enumerating items
    /// in this in-memory store instance) that disallow any concurrent write
    /// operations to the store.
    /// </summary>
    /// <remarks>
    /// Note that this provides at <em>best</em> a simplistic precaution against
    /// concurrent writes while an entry enumeration is ongoing. It is expected
    /// that the Key-Value store that uses this in-memory store provides a higher
    /// level mechanism (such as a reader-writer locking mechanism or even
    /// mutually exclusive access for operations) to properly protect against
    /// memory corruption. The specific strategy however often depends on the
    /// specific application so is not enforced here.
    /// </remarks>
    private long readOnlyOpCount = 0;

    #endregion

    /// <inheritdoc/>
    public int Count => tree.Count;

    /// <inheritdoc/>
    public bool TryGet(TKey key, out StoreEntry<TKey, TValue> entry)
    {
        return tree.TryFind(StoreEntry<TKey, TValue>.Sentinel(key), out entry);
    }

    /// <inheritdoc/>
    public void Set(StoreEntry<TKey, TValue> entry)
    {
        // If any read-only operations (such as enumerating entries) are ongoing,
        // we cannot allow any write operations at this point.
        long refCount = Interlocked.Read(ref readOnlyOpCount);
        if (refCount > 0)
        {
            throw new InvalidOperationException(
                "Cannot write to the store: the store is read-only.");
        }

        tree.Upsert(entry);
    }

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> GetOrderedEnumerator()
        => GetOrderedEnumerator(Range<TKey>.Unbounded);

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> GetOrderedEnumerator(
        Range<TKey> range)
    {
        // Increment the read-only operation count to disallow write operations
        // while the enumerator is in use.
        Interlocked.Increment(ref readOnlyOpCount);

        IEnumerator<StoreEntry<TKey, TValue>> result = tree.GetInOrderEnumerator();

        if (range.IsBounded)
        {
            if (range.HasStart)
            {
                result = new LowerBoundEnumerator<StoreEntry<TKey, TValue>>(
                    result, StoreEntry<TKey, TValue>.Sentinel(range.Start));
            }

            if (range.HasEnd)
            {
                result = new UpperBoundEnumerator<StoreEntry<TKey, TValue>>(
                    result, StoreEntry<TKey, TValue>.Sentinel(range.End));
            }
        }

        result = new GuardingEnumerator<StoreEntry<TKey, TValue>>(
            new ReadRefCountGuard(this), result);

        return result;
    }

    private readonly record struct ReadRefCountGuard(
        DefaultMemoryKeyValueStore<TKey, TValue> Store
        ) : IDisposable
    {
        public void Dispose()
        {
            Interlocked.Decrement(ref Store.readOnlyOpCount);
        }
    }
}
