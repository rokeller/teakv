using System;

namespace TeaSuite.KV.Data;

/// <summary>
/// Defines the contract for a factory of <see cref="IMemoryKeyValueStore{TKey, TValue}"/> objects.
/// </summary>
/// <typeparam name="TKey">
/// Type type of the keys used for entries of the stores created with the factory.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries of the stores created with the factory.
/// </typeparam>
public interface IMemoryKeyValueStoreFactory<TKey, TValue> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Creates a new instance of <see cref="IMemoryKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="IMemoryKeyValueStore{TKey, TValue}"/>.
    /// </returns>
    IMemoryKeyValueStore<TKey, TValue> Create();
}
