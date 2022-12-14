namespace TeaSuite.KV;

/// <summary>
/// Defines the contract for a Key/Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public interface IKeyValueStore<TKey, TValue>
{
    /// <summary>
    /// Tries to get the value for the given <paramref name="key"/> from the store.
    /// </summary>
    /// <param name="key">
    /// A value of <typeparamref name="TKey"/> that identifies the entry for which to get the value.
    /// </param>
    /// <param name="value">
    /// If an entry was found, holds a value of <typeparamref name="TValue"/> that represents the value of the entry.
    /// </param>
    /// <returns>
    /// True if an entry was found, false otherwise.
    /// </returns>
    bool TryGet(TKey key, out TValue? value);

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

    /// <summary>
    /// Closes the Key/Value store. When this method finishes, the store can no longer be used for reads or writes.
    /// </summary>
    void Close();
}
