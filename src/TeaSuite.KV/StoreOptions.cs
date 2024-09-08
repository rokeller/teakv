using System;

namespace TeaSuite.KV;

/// <summary>
/// Defines options for a Key-Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key-Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key-Value store.
/// </typeparam>
public class StoreOptions<TKey, TValue> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Gets or sets the <see cref="StoreSettings"/> for the store.
    /// </summary>
    public StoreSettings Settings { get; set; } = new();
}
