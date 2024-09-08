using System;

namespace TeaSuite.KV;

/// <summary>
/// Defines an entry in the Key-Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used by the entry.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value used by the entry.
/// </typeparam>
public readonly partial struct StoreEntry<TKey, TValue> :
    IComparable<StoreEntry<TKey, TValue>>
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
    /// A <see cref="StoreEntry{TKey, TValue}"/> which marks the given <paramref name="key"/>
    /// as deleted.
    /// </returns>
    public static StoreEntry<TKey, TValue> Delete(TKey key)
    {
        return new(key);
    }

    /// <summary>
    /// Creates a sentinel entry for the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">
    /// The key for which to create a sentinel entry.
    /// </param>
    /// <returns>
    /// A <see cref="StoreEntry{TKey, TValue}"/> which serves as sentinel for the
    /// given <paramref name="key"/>.
    /// </returns>
    public static StoreEntry<TKey, TValue> Sentinel(TKey key)
    {
        // Currently, entry comparison is done purely on keys, which is why an
        // entry that's marked as deleted is a good-enough sentinel.
        return new(key);
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

    /// <summary>
    /// A predicate that checks if the given <paramref name="entry"/> is marked
    /// as deleted.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to check.
    /// </param>
    /// <returns>
    /// True if the <paramref name="entry"/> is deleted, or false otherwise.
    /// </returns>
    public static bool IsNotDeleted(StoreEntry<TKey, TValue> entry)
    {
        return !entry.IsDeleted;
    }
}
