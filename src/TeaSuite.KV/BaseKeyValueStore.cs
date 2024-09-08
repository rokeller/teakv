using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

/// <summary>
/// Base class for implementations of <see cref="IKeyValueStore{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key-Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key-Value store.
/// </typeparam>
public abstract class BaseKeyValueStore<TKey, TValue> :
    IKeyValueStore<TKey, TValue>
    where TKey : IComparable<TKey>
{
    private readonly ILogger<BaseKeyValueStore<TKey, TValue>> logger;
    private readonly ISegmentManager<TKey, TValue> segmentManager;

    /// <summary>
    /// Tracks the current set of segments sorted by age (youngest first).
    /// </summary>
    private volatile ImmutableSortedSet<Segment<TKey, TValue>> segments;

    /// <summary>
    /// Tracks the ID of the last segment observed by this store.
    /// </summary>
    private long lastSegmentId = -1;

    /// <summary>
    /// Initializes a new instance of <see cref="BaseKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger{TCategoryName}"/> to use.
    /// </param>
    /// <param name="segmentManager">
    /// The <see cref="ISegmentManager{TKey, TValue}"/> to use to manage persisted segments of the store.
    /// </param>
    protected BaseKeyValueStore(
        ILogger<BaseKeyValueStore<TKey, TValue>> logger,
        ISegmentManager<TKey, TValue> segmentManager)
    {
        this.logger = logger;
        this.segmentManager = segmentManager;

        Segment<TKey, TValue> TrackLastSegmentId(Segment<TKey, TValue> segment)
        {
            if (segment.Id > lastSegmentId)
            {
                lastSegmentId = segment.Id;
            }

            return segment;
        }

        segments = ImmutableSortedSet.CreateRange(segmentManager.DiscoverSegments().Select(TrackLastSegmentId));
    }

    /// <inheritdoc/>
    public abstract bool TryGet(TKey key, out TValue? value);

    /// <inheritdoc/>
    public abstract void Set(TKey key, TValue value);

    /// <inheritdoc/>
    public abstract void Delete(TKey key);

    /// <inheritdoc/>
    public virtual void Close()
    {
        // There's nothing to flush, or clean up.
    }

    #region Scanning

    /// <inheritdoc/>
    public virtual IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator()
    {
        return GetEntriesEnumerator(Range<TKey>.Unbounded);
    }

    /// <inheritdoc/>
    public virtual IEnumerator<StoreEntry<TKey, TValue>> GetEntriesEnumerator(
        Range<TKey> range
        )
    {
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> materializedEnumerators =
            GetEntriesEnumerators(range);

        return CreateEntriesEnumerator(materializedEnumerators);
    }

    /// <inheritdoc/>
    public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return GetEnumerator(Range<TKey>.Unbounded);
    }

    /// <inheritdoc/>
    public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator(Range<TKey> range)
    {
        IEnumerator<StoreEntry<TKey, TValue>> all = GetEntriesEnumerator(range);

        // Since we merge all currently known segments, we can skip those entries
        // that are deleted.
        FilteringEnumerator<StoreEntry<TKey, TValue>> filtered =
            new(all, StoreEntry<TKey, TValue>.IsNotDeleted);

        return new TransformingEnumerator<StoreEntry<TKey, TValue>, KeyValuePair<TKey, TValue>>(
            filtered, Convert);
    }

    #endregion

    /// <summary>
    /// Gets the <see cref="ILogger{TCategoryName}"/> to use.
    /// </summary>
    protected ILogger<BaseKeyValueStore<TKey, TValue>> Logger => logger;

    /// <summary>
    /// Gets the <see cref="ISegmentManager{TKey, TValue}"/> to use to manage persisted segments of the store.
    /// </summary>
    protected ISegmentManager<TKey, TValue> SegmentManager => segmentManager;

    /// <summary>
    /// Gets the current set of segments sorted by age (youngest first).
    /// </summary>
    protected ImmutableSortedSet<Segment<TKey, TValue>> Segments { get => segments; set => segments = value; }

    /// <summary>
    /// Gets the ID of the next segment to allocate for this store.
    /// </summary>
    /// <returns>
    /// A <see cref="long"/> value representing the segment ID.
    /// </returns>
    protected long GetNextSegmentId()
    {
        return Interlocked.Increment(ref lastSegmentId);
    }

    /// <summary>
    /// Tries to get the <see cref="StoreEntry{TKey, TValue}"/> for the given <paramref name="key"/> from the store's
    /// current segments.
    /// </summary>
    /// <param name="key">
    /// The key of the entry to get.
    /// </param>
    /// <param name="entry">
    /// If successful, holds the most recent <see cref="StoreEntry{TKey, TValue}"/> that was found.
    /// </param>
    /// <returns>
    /// True if successful, false otherwise.
    /// </returns>
    protected bool TryGetFromSegments(TKey key, out StoreEntry<TKey, TValue> entry)
    {
        foreach (Segment<TKey, TValue> segment in segments)
        {
            if (TryGetFromSegment(segment, key, out entry))
            {
                return true;
            }
        }

        entry = default;

        return false;
    }

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> of <see cref="Segment{TKey, TValue}"/>
    /// in the order in which they should be searched, i.e. typically from
    /// youngest to oldest.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> limiting the search in segments.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="Segment{TKey, TValue}"/>
    /// to search in the order in which they are returned.
    /// </returns>
    protected virtual IEnumerable<Segment<TKey, TValue>> GetSegmentsToSearch(
        Range<TKey> range)
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

        return segments;
    }

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> of <see cref="IEnumerator{T}"/>
    /// that returns <see cref="StoreEntry{TKey, TValue}"/> values for
    /// enumerating entries in a Key-Value store. The enumerators must be in
    /// the order of precedence, i.e. the youngest first, the oldest last.
    /// </summary>
    /// <param name="range">
    /// The <see cref="Range{T}"/> the enumerators are to be created for.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="IEnumerator{T}"/>.
    /// </returns>
    protected virtual IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> GetEntriesEnumerators(
        Range<TKey> range)
    {
        // Get an ordered list of per-segment enumerators, with the youngest
        // segment first.
        IEnumerable<Segment<TKey, TValue>> segments = GetSegmentsToSearch(range);
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> enumerators =
            segments.Select(s => s.Driver.GetEntryEnumerator(range));
        // Materialize the enumerators so they are all initialized.
        List<IEnumerator<StoreEntry<TKey, TValue>>> materializedEnumerators =
            enumerators.ToList();

        return materializedEnumerators;
    }

    /// <summary>
    /// Creates an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that can be used
    /// </summary>
    /// <param name="enumerators"></param>
    /// <returns></returns>
    protected virtual IEnumerator<StoreEntry<TKey, TValue>> CreateEntriesEnumerator(
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> enumerators
        )
    {
        MergingEnumerator<StoreEntry<TKey, TValue>> merging = new(enumerators);

        return merging;
    }

    /// <summary>
    /// Converts a <see cref="StoreEntry{TKey, TValue}"/> into a
    /// <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to convert.
    /// </param>
    /// <returns>
    /// A <see cref="KeyValuePair{TKey, TValue}"/> value that represents the
    /// given <paramref name="entry"/>.
    /// </returns>
    protected static KeyValuePair<TKey, TValue> Convert(StoreEntry<TKey, TValue> entry)
    {
        Debug.Assert(!entry.IsDeleted, "The entry must not be deleted.");
        return new(entry.Key, entry.Value!);
    }

    /// <summary>
    /// Tries to get the <see cref="StoreEntry{TKey, TValue}"/> for the given <paramref name="key"/> from the specified
    /// <paramref name="segment"/>.
    /// </summary>
    /// <param name="segment">
    /// The <see cref="Segment{TKey, TValue}"/> from which to get the entry.
    /// </param>
    /// <param name="key">
    /// The key of the entry to get.
    /// </param>
    /// <param name="entry">
    /// If successful, holds the most recent <see cref="StoreEntry{TKey, TValue}"/> that was found.
    /// </param>
    /// <returns>
    /// True if successful, false otherwise.
    /// </returns>
    private bool TryGetFromSegment(Segment<TKey, TValue> segment, TKey key, out StoreEntry<TKey, TValue> entry)
    {
        ValueTask<StoreEntry<TKey, TValue>?> getTask = segment.Driver.GetEntryAsync(key, default);
        StoreEntry<TKey, TValue>? foundEntry = getTask.GetValueTaskResult();

        if (foundEntry.HasValue)
        {
            logger.LogTrace("Found entry for key '{key}' in segment {segmentId}.", key, segment.Id);
            entry = foundEntry.Value;
            return true;
        }

        entry = default;
        return false;
    }
}
