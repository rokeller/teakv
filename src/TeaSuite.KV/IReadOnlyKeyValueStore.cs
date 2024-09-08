using System;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Defines the contract for a read-only Key-Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key-Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key-Value store.
/// </typeparam>
public interface IReadOnlyKeyValueStore<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Tries to get the value for the given <paramref name="key"/> from the
    /// store.
    /// </summary>
    /// <param name="key">
    /// A value of <typeparamref name="TKey"/> that identifies the entry for
    /// which to get the value.
    /// </param>
    /// <param name="value">
    /// If an entry was found, holds a value of <typeparamref name="TValue"/>
    /// that represents the
    /// value of the entry.
    /// </param>
    /// <returns>
    /// True if an entry was found, false otherwise.
    /// </returns>
    bool TryGet(TKey key, out TValue? value);

    /// <summary>
    /// Closes the Key-Value store. When this method finishes, the store can no
    /// longer be used for reads.
    /// </summary>
    void Close();

    #region Scanning

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// items representing the entries in the store, including deleted ones.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>.
    /// </returns>
    IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator();

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// items representing the entries in the given <paramref name="range"/> in
    /// the store, including deleted ones.
    /// </summary>
    /// <param name="range">
    /// A <see cref="Range{T}"/> of <typeparamref name="TKey"/> that defines the
    /// range of key/value pairs to get.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>.
    /// </returns>
    IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator(Range<TKey> range);

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>
    /// items representing the key/value pairs in the store.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </returns>
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>
    /// items representing the key/value pairs in the given <paramref name="range"/>
    /// in the store.
    /// </summary>
    /// <param name="range">
    /// A <see cref="Range{T}"/> of <typeparamref name="TKey"/> that defines the
    /// range of key/value pairs to get.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </returns>
    IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(Range<TKey> range);

    #endregion
}
