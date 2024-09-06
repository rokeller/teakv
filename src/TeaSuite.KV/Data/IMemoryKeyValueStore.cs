using System;
using System.Collections.Generic;

namespace TeaSuite.KV.Data;

/// <summary>
/// Defines the interface for an in-memory Key-Value store.
/// </summary>
/// <typeparam name="TKey">
/// Type type of the keys used for entries of the store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries of the store.
/// </typeparam>
public interface IMemoryKeyValueStore<TKey, TValue> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Gets the current number of entries in the store.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Tries to get the <see cref="StoreEntry{TKey, TValue}"/> for the given
    /// <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// The key for which to find the entry.
    /// </param>
    /// <param name="entry">
    /// If successful, holds the <see cref="StoreEntry{TKey, TValue}"/> on return.
    /// </param>
    /// <returns>
    /// True if successful, or false otherwise.
    /// </returns>
    bool TryGet(TKey key, out StoreEntry<TKey, TValue> entry);

    /// <summary>
    /// Sets (adds or updates) the given <paramref name="entry"/>.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to set in the store.
    /// </param>
    void Set(StoreEntry<TKey, TValue> entry);

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that represents all the current entries of the store in ascending order.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that can be used to enumerate all the entries of the store in ascending
    /// order.
    /// </returns>
    IEnumerator<StoreEntry<TKey, TValue>> GetOrderedEnumerator();

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that represents all the current entries in the given <paramref name="range"/>
    /// of the store in ascending order.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> of keys to enumerate.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that can be used to enumerate all the entries in the given
    /// <paramref name="range"/> of the store in ascending order.
    /// </returns>
    IEnumerator<StoreEntry<TKey, TValue>> GetOrderedEnumerator(Range<TKey> range);
}
