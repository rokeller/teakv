using System;
using System.Collections.Immutable;
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
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public abstract class BaseKeyValueStore<TKey, TValue> : IKeyValueStore<TKey, TValue> where TKey : IComparable<TKey>
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
