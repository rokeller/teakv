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
        new AvlTree<StoreEntry<TKey, TValue>>(StoreEntry<TKey, TValue>.KeyComparer);

    /// <summary>
    /// Holds the current state of the tree.
    /// </summary>
    private long state = StateReadWrite;

    #endregion

    /// <inheritdoc/>
    public int Count => tree.Count;

    /// <inheritdoc/>
    public bool TryGet(TKey key, out StoreEntry<TKey, TValue> entry)
    {
        // The Find method of the tree only uses the StoreEntry.KeyComparer, which only compares keys. Therefore,
        // it doesn't matter what entry we pass, as long as the keys match.
        return tree.TryFind(StoreEntry<TKey, TValue>.Delete(key), out entry);
    }

    /// <inheritdoc/>
    public void Set(StoreEntry<TKey, TValue> entry)
    {
        // If enumeration of items in the tree has begun, we really shouldn't make any changes to the tree anymore.
        long curState = Interlocked.Read(ref state);
        if (curState == StateReadOnly)
        {
            throw new InvalidOperationException("Cannot write to the store: the store is read-only.");
        }

        tree.Upsert(entry);
    }

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> GetOrderedEnumerator()
    {
        // Enumeration of items in the tree requires the tree to be made read-only, otherwise enumeration would not be
        // stable.
        long oldState = Interlocked.CompareExchange(ref state, StateReadOnly, StateReadWrite);
        if (oldState == StateReadOnly)
        {
            throw new InvalidOperationException("An ordered enumerator has already been requested before.");
        }

        return tree.GetInOrderEnumerator();
    }
}
