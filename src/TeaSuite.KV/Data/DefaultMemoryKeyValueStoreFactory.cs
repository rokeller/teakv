using System;

namespace TeaSuite.KV.Data;

/// <summary>
/// Implements <see cref="IMemoryKeyValueStoreFactory{TKey, TValue}"/> by creating instances of
/// <see cref="DefaultKeyValueStore{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">
/// Type type of the keys used for entries of the stores created with the factory.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries of the stores created with the factory.
/// </typeparam>
public sealed class DefaultMemoryKeyValueStoreFactory<TKey, TValue> : IMemoryKeyValueStoreFactory<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <inheritdoc/>
    public IMemoryKeyValueStore<TKey, TValue> Create()
    {
        return new DefaultMemoryKeyValueStore<TKey, TValue>();
    }
}
