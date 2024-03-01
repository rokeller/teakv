using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

/// <summary>
/// Implements a read-only instance of the Key/Value store. Attempts to write to
/// this store will result in <see cref="NotSupportedException"/> being thrown.
/// This implies that an in-memory store will <strong>not</strong> be used.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public class ReadOnlyKeyValueStore<TKey, TValue> :
    BaseKeyValueStore<TKey, TValue>,
    IReadOnlyKeyValueStore<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <inheritdoc/>
    public ReadOnlyKeyValueStore(
        ILogger<ReadOnlyKeyValueStore<TKey, TValue>> logger,
        ISegmentManager<TKey, TValue> segmentManager)
        : base(logger, segmentManager)
    { }

    /// <inheritdoc/>
    public override bool TryGet(TKey key, out TValue? value)
    {
        StoreEntry<TKey, TValue> entry;

        // There's no in-memory store (because it would remain empty), so check
        // the segments right away.
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
    public IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator()
    {
        return GetEntriesEnumerator(Range<TKey>.Unbounded());
    }

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator(
        Range<TKey> range
        )
    {
        // Get an ordered list of per-segment enumerators, with the youngest
        // segment first.
        IEnumerable<Segment<TKey, TValue>> segments = Segments;

        if (range.IsBounded)
        {
            // When the range is bounded, let's take only those segments that
            // actually overlap with the range. All other segments by definition
            // wouldn't contribute to the output.
            segments = segments.Where(segment => segment.Driver.Overlaps(range));
        }

        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> enumerators =
            segments.Select(s => s.Driver.GetEntryEnumerator(range));

        List<IEnumerator<StoreEntry<TKey, TValue>>> materializedEnumerators =
            enumerators.ToList();
        MergingEnumerator<StoreEntry<TKey, TValue>> merging =
            new MergingEnumerator<StoreEntry<TKey, TValue>>(
                materializedEnumerators.ToArray());

        return merging;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return GetEnumerator(Range<TKey>.Unbounded());
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(Range<TKey> range)
    {
        IEnumerator<StoreEntry<TKey, TValue>> all = GetEntriesEnumerator(range);

        // Since we merge all currently known segments, we can skip those entries
        // that are deleted.
        FilteringEnumerator<StoreEntry<TKey, TValue>> filtered =
            new FilteringEnumerator<StoreEntry<TKey, TValue>>(
                all,
                StoreEntry<TKey, TValue>.IsNotDeleted);

        return new TransformingEnumerator<StoreEntry<TKey, TValue>, KeyValuePair<TKey, TValue>>(
            filtered, Convert);
    }

    /// <inheritdoc/>
    public override void Set(TKey key, TValue value)
    {
        throw new NotSupportedException(
            "Setting values is not supported in a ReadOnlyKeyValueStore.");
    }

    /// <inheritdoc/>
    public override void Delete(TKey key)
    {
        throw new NotSupportedException(
            "Deleting values is not supported in a ReadOnlyKeyValueStore.");
    }

    private static KeyValuePair<TKey, TValue> Convert(StoreEntry<TKey, TValue> entry)
    {
        Debug.Assert(!entry.IsDeleted, "The entry must not be deleted.");
        return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value!);
    }
}
