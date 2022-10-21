using System;
using Microsoft.Extensions.Logging;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

/// <summary>
/// Implements a read-only instance of the Key/Value store. Attempts to write to this store will result in
/// <see cref="NotSupportedException"/> being thrown. This implies that an in-memory store will not be used.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public class ReadOnlyKeyValueStore<TKey, TValue> : BaseKeyValueStore<TKey, TValue> where TKey : IComparable<TKey>
{
    public ReadOnlyKeyValueStore(
        ILogger<ReadOnlyKeyValueStore<TKey, TValue>> logger,
        ISegmentManager<TKey, TValue> segmentManager)
        : base(logger, segmentManager)
    { }

    /// <inheritdoc/>
    public override bool TryGet(TKey key, out TValue? value)
    {
        StoreEntry<TKey, TValue> entry;

        // There's no in-memory store (because it would remain empty), so check the segments right away.
        if (!TryGetFromSegments(key, out entry))
        {
            value = default;
            return false;
        }

        if (entry.IsDeleted)
        {
            value = default;
            return false;
        }
        else
        {
            value = entry.Value;
            return true;
        }
    }

    /// <inheritdoc/>
    public override void Set(TKey key, TValue value)
    {
        throw new NotSupportedException("Setting values is not supported in a ReadOnlyKeyValueStore.");
    }

    /// <inheritdoc/>
    public override void Delete(TKey key)
    {
        throw new NotSupportedException("Deleting values is not supported in a ReadOnlyKeyValueStore.");
    }
}
