using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// values representing all the entries from the segment the driver is for.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that can be used to enumerate all the segment's entries.
    /// </returns>
    public IEnumerator<StoreEntry<TKey, TValue>> GetEntryEnumerator()
    {
        if (reader == null)
        {
            throw new InvalidOperationException("Cannot read in a non-readable segment.");
        }
        ThrowIfDisposed();

        return new EntryEnumerator(this, default);
    }

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreBuilder{TKey, TValue}"/>
    /// values representing the entries from the given <paramref name="range"/>.
    /// </summary>
    /// <param name="range">
    /// The key range to enumerate.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that can be used to enumerate all the segment's entries in the given
    /// <paramref name="range"/>.
    /// </returns>
    public IEnumerator<StoreEntry<TKey, TValue>> GetEntryEnumerator(Range<TKey> range)
    {
        if (reader == null)
        {
            throw new InvalidOperationException("Cannot read in a non-readable segment.");
        }
        ThrowIfDisposed();

        if (!Overlaps(range))
        {
            // The range does not overlap with the segment. Return an empty
            // enumerator.
            return GetEmptyEnumerator();
        }

        (long startOffset, long? scanSize) = GetScanRange(range);
        IEnumerator<StoreEntry<TKey, TValue>> result = new EntryEnumerator(
            this, startOffset, scanSize, default);

        if (range.HasStart)
        {
            result = new LowerBoundEnumerator<StoreEntry<TKey, TValue>>(
                result, StoreEntry<TKey, TValue>.Sentinel(range.Start));
        }

        if (range.HasEnd)
        {
            result = new UpperBoundEnumerator<StoreEntry<TKey, TValue>>(
                result, StoreEntry<TKey, TValue>.Sentinel(range.End));
        }

        return result;
    }

    /// <summary>
    /// Checks if the current segment overlaps partially or entirely with the
    /// given key range.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> of <typeparamref name="TKey"/> to check.
    /// </param>
    /// <returns>
    /// True if the segment overlaps with the <paramref name="range"/>, or false
    /// otherwise.
    /// </returns>
    public bool Overlaps(Range<TKey> range)
    {
        if (reader == null)
        {
            throw new InvalidOperationException("Cannot read in a non-readable segment.");
        }
        ThrowIfDisposed();

        if (!range.IsBounded)
        {
            // If the range is not bounded, the segment overlaps the range by
            // definition.
            return true;
        }

        // The range overlaps if the range's start is before the segment's end,
        // and the range's end is after the segment's start.

        if (range.HasStart)
        {
            if (range.Start.CompareTo(LastIndexEntry!.Value.Key) > 0)
            {
                // The desired range's start is after the segment's last key.
                // The range cannot possibly overlap.
                return false;
            }
        }

        if (range.HasEnd)
        {
            if (range.End.CompareTo(FirstIndexEntry!.Value.Key) <= 0)
            {
                // The desired range's end is before the segment's first key.
                // The range cannot possibly overlap.
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the range (start offset and size) of the range to scan for the given
    /// <paramref name="range"/>.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> that should be scanned.
    /// </param>
    /// <returns>
    /// A tuple of offset and optional size (in bytes) of the range to scan.
    /// </returns>
    private (long startOffset, long? scanSize) GetScanRange(Range<TKey> range)
    {
        long startOffset = 0;
        long? scanSize = null;

        if (range.HasStart)
        {
            IndexEntry? start = FindLastIndexEntryBeforeKey(range.Start, CancellationToken.None);
            if (start.HasValue)
            {
                startOffset = start.Value.Position;
                logger.LogTrace("Start scan on block {start}.", start.Value);
            }
        }

        if (range.HasEnd)
        {
            IndexEntry? end = FindLastIndexEntryBeforeKey(range.End, CancellationToken.None);

            if (end.HasValue)
            {
                // The IndexEntry end currently points to the block containing the
                // end of the range, so we need to look for the next index entry
                // to know how much data to read at most -- unless the end key is
                // right at the beginning of the block.
                if (end.Value.Key.CompareTo(range.End) != 0)
                {
                    end = GetNextIndexEntry(end.Value);
                }
            }

            if (end.HasValue)
            {
                scanSize = end.Value.Position - startOffset;
                logger.LogTrace("Finish scan on block {end}; scan size: {size} bytes.",
                    end.Value, scanSize);
            }
        }

        return (startOffset, scanSize);
    }
}
