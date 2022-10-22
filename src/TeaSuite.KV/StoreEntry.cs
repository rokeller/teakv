using System;
namespace TeaSuite.KV;

/// <summary>
/// Defines an entry in the Key/Value Store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used by the entry.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value used by the entry.
/// </typeparam>
public readonly partial struct StoreEntry<TKey, TValue> : IComparable<StoreEntry<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Initializes a new non-deleted instance of StoreEntry.
    /// </summary>
    /// <param name="key">
    /// The key of the entry.
    /// </param>
    /// <param name="value">
    /// The value of the entry.
    /// </param>
    public StoreEntry(TKey key, TValue value)
    {
        Key = key;
        Value = value;
        IsDeleted = false;
    }

    /// <summary>
    /// Initializes a new deleted instance of StoreEntry.
    /// </summary>
    /// <param name="key">
    /// The key of the entry.
    /// </param>
    private StoreEntry(TKey key)
    {
        Key = key;
        Value = default;
        IsDeleted = true;
    }

    /// <summary>
    /// Creates a deleted entry for the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// The key for which to create a deleted entry.
    /// </param>
    /// <returns>
    /// A <see cref="StoreEntry{TKey, TValue}"/> which marks the given <paramref name="key"/> as deleted.
    /// </returns>
    public static StoreEntry<TKey, TValue> Delete(TKey key)
    {
        return new StoreEntry<TKey, TValue>(key);
    }

    /// <inheritdoc/>
    public int CompareTo(StoreEntry<TKey, TValue> other)
    {
        return Key.CompareTo(other.Key);
    }

    /// <summary>
    /// Gets the key of the entry.
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Gets the value of the entry, or null if the entry is deleted.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets a flag which indicates whether the entry is deleted.
    /// </summary>
    public bool IsDeleted { get; }
}
