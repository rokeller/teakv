using System;

namespace TeaSuite.KV;

/// <summary>
/// Defines the contract for a Key-Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key-Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key-Value store.
/// </typeparam>
public interface IKeyValueStore<TKey, TValue> :
    IReadOnlyKeyValueStore<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Sets the <paramref name="value"/> for the entry with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// A value of <typeparamref name="TKey"/> that identifies the entry for which to set the value.
    /// </param>
    /// <param name="value">
    /// A value of <typeparamref name="TValue"/> that represents the value to set for the entry.
    /// </param>
    void Set(TKey key, TValue value);

    /// <summary>
    /// Deletes the entry with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// A value of <typeparamref name="TKey"/> that identifies the entry to delete.
    /// </param>
    void Delete(TKey key);
}
