using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeaSuite.KV.IO.Formatters;
using TeaSuite.KV.Policies;

namespace TeaSuite.KV.IO;

/// <summary>
/// Represents a driver for Key-Value store segments.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used in the store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used in the store.
/// </typeparam>
public sealed partial class Driver<TKey, TValue> : IDisposable, IAsyncDisposable where TKey : IComparable<TKey>
{
    #region Fields

    private readonly ILogger<Driver<TKey, TValue>> logger;

    /// <summary>
    /// The <see cref="ISegmentReader"/> to use; only set when the driver is in read-only mode.
    /// </summary>
    private readonly ISegmentReader? reader;

    /// <summary>
    /// The <see cref="ISegmentWriter"/> to use; only set when the driver is in write-only mode.
    /// </summary>
    private readonly ISegmentWriter? writer;
    private readonly IEntryFormatter<TKey, TValue> formatter;
    private bool isDisposed;

    #endregion

    #region C'tors

    /// <summary>
    /// Initializes a new instance of Driver for reading a segment.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger{TCategoryName}"/> to use.
    /// </param>
    /// <param name="reader">
    /// The <see cref="ISegmentReader"/> to use to read the segment.
    /// </param>
    /// <param name="formatter">
    /// The <see cref="IEntryFormatter{TKey, TValue}"/> to use to parse keys and values from the segment.
    /// </param>
    public Driver(
        ILogger<Driver<TKey, TValue>> logger,
        ISegmentReader reader,
        IEntryFormatter<TKey, TValue> formatter)
    {
        this.logger = logger;
        this.reader = reader;
        this.formatter = formatter;

        // Read the index into memory, so lookups are made faster.
        List<IndexEntry> index = ReadIndex(out SegmentMetadata metadata, default);
        Metadata = metadata;
        logger.LogDebug("Loading segment of version {version} with flags {flags} from {timestamp:O}.",
            metadata.Version, metadata.Flags, metadata.Timestamp);

        IndexEntry[] pool = ArrayPool<IndexEntry>.Shared.Rent(index.Count);
        index.CopyTo(pool);
        // Remember the first and last entry of the segment so we can skip all lookups not falling into the range.
        FirstIndexEntry = index[0];
        LastIndexEntry = index[index.Count - 1];
        Index = new(pool, 0, index.Count);
    }

    /// <summary>
    /// Initializes a new instance of Driver for writing a segment.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger{TCategoryName}"/> to use.
    /// </param>
    /// <param name="writer">
    /// The <see cref="ISegmentWriter"/> to use to write the segment.
    /// </param>
    /// <param name="formatter">
    /// The <see cref="IEntryFormatter{TKey, TValue}"/> to use to write keys and values to the segment.
    /// </param>
    public Driver(
        ILogger<Driver<TKey, TValue>> logger,
        ISegmentWriter writer,
        IEntryFormatter<TKey, TValue> formatter)
    {
        this.logger = logger;
        this.writer = writer;
        this.formatter = formatter;
    }

    #endregion

    #region Internal Properties

    /// <summary>
    /// Gets the <see cref="SegmentMetadata"/> describing the segment's metadata; only set when the driver is in
    /// read-only mode.
    /// </summary>
    internal SegmentMetadata? Metadata { get; }

    /// <summary>
    /// Gets an <see cref="ArraySegment{T}"/> of <see cref="IndexEntry"/> describing the index of the segment; only set
    /// when the driver is in read-only mode.
    /// </summary>
    internal ArraySegment<Driver<TKey, TValue>.IndexEntry>? Index { get; }

    /// <summary>
    /// Gets the <see cref="IndexEntry"/> describing the first entry in the segment; only set when the driver is in
    /// read-only mode.
    /// </summary>
    internal IndexEntry? FirstIndexEntry { get; }

    /// <summary>
    /// Gets the <see cref="IndexEntry"/> describing the last entry in the segment; only set when the driver is in
    /// read-only mode.
    /// </summary>
    internal IndexEntry? LastIndexEntry { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Asynchronously tries to get the entry with the given <paramref name="key"/> from the segment.
    /// </summary>
    /// <param name="key">
    /// The <typeparamref name="TKey"/> value of the key to look up.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <see cref="StoreEntry{TKey, TValue}"/> that tracks the outcome of the
    /// operation. The result will be null only if an entry for the given <paramref name="key"/> was not found.
    /// </returns>
    public async ValueTask<StoreEntry<TKey, TValue>?> GetEntryAsync(TKey key, CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new InvalidOperationException("Cannot read in a non-readable segment.");
        }
        ThrowIfDisposed();

        // Look for the last entry on the index that is before the given key. Only if such an entry exists do we
        // actually need to look any further for such an entry in the data file.
        IndexEntry? startEntry = FindLastIndexEntryBeforeKey(key, cancellationToken);
        if (!startEntry.HasValue)
        {
            return null;
        }

        return await GetEntryAsync(startEntry.Value, key, cancellationToken).ConfigureAwaitLib();
    }

    /// <summary>
    /// Asynchronously writes the <paramref name="entries"/> using the specified <paramref name="settings"/>.
    /// </summary>
    /// <param name="entries">
    /// An <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/> that can be used to enumerate all the
    /// entries to write.
    /// </param>
    /// <param name="settings">
    /// An instance of <see cref="StoreSettings"/> that refers to the <see cref="IIndexPolicy"/> to use when indexing.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that results in a <see cref="long"/> value describing how many entries were
    /// written to the new segment.
    /// </returns>
    public async Task<long> WriteEntriesAsync(
        IEnumerator<StoreEntry<TKey, TValue>> entries,
        StoreSettings settings,
        CancellationToken cancellationToken)
    {
        if (writer == null)
        {
            throw new InvalidOperationException("Cannot write in a non-writable segment.");
        }
        ThrowIfDisposed();

        long lastIndexedEntryIndex = 0;
        long lastIndexedEntryOffset = 0;
        int curIndexId = 0;
        long curEntryIndex = 0;
        long? curDataPos = null;
        StoreEntry<TKey, TValue>? lastEntry = null;

        await using Stream indexStream = await writer
            .OpenIndexForWriteAsync(cancellationToken).ConfigureAwaitLib();
        await using Stream dataStream = await writer
            .OpenDataForWriteAsync(cancellationToken).ConfigureAwaitLib();
        await using WriteContext indexContext = new(indexStream);
        await using WriteContext dataContext = new(dataStream);

        WriteSegmentMetadata(indexContext, SegmentMetadata.New());

        while (entries.MoveNext())
        {
            StoreEntry<TKey, TValue> entry = entries.Current;

            curDataPos = dataStream.Position;
            long offsetBytes = curDataPos.Value - lastIndexedEntryOffset;
            long offsetEntries = curEntryIndex - lastIndexedEntryIndex;
            ValueTask pendingIndexWrite = default;
            lastEntry = entry;

            if (curEntryIndex == 0 || // Always index the first entry.
                settings.IndexPolicy.ShouldIndex(offsetBytes, offsetEntries, curEntryIndex))
            {
                logger.LogDebug("Indexing entry with index {index} at position {offset}.",
                    curEntryIndex, curDataPos.Value);

                pendingIndexWrite = WriteIndexEntryAsync(
                    indexContext,
                    new(curIndexId++, entry.Key, curDataPos.Value),
                    cancellationToken);

                // Remember the index and position (in the data stream) of this entry for future evaluations of the
                // index policy.
                lastIndexedEntryIndex = curEntryIndex;
                lastIndexedEntryOffset = curDataPos.Value;
            }

            ValueTask pendingDataWrite = WriteEntryAsync(dataContext, entry, cancellationToken);

            await pendingIndexWrite.ConfigureAwaitLib();
            await pendingDataWrite.ConfigureAwaitLib();

            curEntryIndex++;
        }

        // If we haven't added an entry on the index for the last written segment entry yet, let's do so now.
        if (lastEntry.HasValue && lastIndexedEntryIndex < curEntryIndex - 1)
        {
            logger.LogDebug("Indexing last entry at position {offset}.", curDataPos!.Value);
            IndexEntry indexEntry = new(curIndexId, lastEntry.Value.Key, curDataPos!.Value);
            await WriteIndexEntryAsync(indexContext, indexEntry, cancellationToken).ConfigureAwaitLib();
        }

        return curEntryIndex;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        ValueTask? disposeReader = reader?.DisposeAsync();
        ValueTask? disposeWriter = writer?.DisposeAsync();

        if (Index.HasValue)
        {
            ArrayPool<IndexEntry>.Shared.Return(Index.Value.Array!);
        }

        if (disposeReader.HasValue)
        {
            await disposeReader.Value.ConfigureAwaitLib();
        }
        if (disposeWriter.HasValue)
        {
            await disposeWriter.Value.ConfigureAwaitLib();
        }

        Dispose(disposing: false);
    }

    #endregion

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    /// <param name="disposing">
    /// A flag that indicates whether the method is called from <see cref="Dispose()"/>.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                reader?.Dispose();
                writer?.Dispose();

                if (Index.HasValue)
                {
                    ArrayPool<IndexEntry>.Shared.Return(Index.Value.Array!);
                }
            }

            isDisposed = true;
        }
    }

    /// <summary>
    /// Reads the index from the current segment's index file.
    /// </summary>
    /// <param name="metadata">
    /// On return, holds the <see cref="SegmentMetadata"/> describing the segment's metadata.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="List{T}"/> of <see cref="IndexEntry"/> values representing all the index entries that were read.
    /// </returns>
    private List<IndexEntry> ReadIndex(out SegmentMetadata metadata, CancellationToken cancellationToken)
    {
        Debug.Assert(reader != null, "The reader must not be null.");
        using Stream stream = reader.OpenIndexForReadAsync(cancellationToken).GetValueTaskResult();
        using ReadContext context = new(stream);
        int indexId = 0;

        metadata = ReadSegmentMetadata(context);
        List<IndexEntry> entries = new();

        while (true)
        {
            ValueTask<IndexEntry?> readTask = ReadIndexEntryAsync(indexId++, context, cancellationToken);
            IndexEntry? entry = readTask.GetValueTaskResult();

            if (entry.HasValue)
            {
                entries.Add(entry.Value);
            }
            else
            {
                break;
            }
        }

        return entries;
    }

    /// <summary>
    /// Finds the last <see cref="IndexEntry"/> (the entry with the highest key lower than the given
    /// <paramref name="key"/>) in the index, if such an entry exists.
    /// </summary>
    /// <param name="key">
    /// A <typeparamref name="TKey"/> value that represents the key that is to be found.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// An <see cref="IndexEntry"/> value representing the matching key from the index, or <c>null</c> if the given
    /// <paramref name="key"/> is out of range for this driver's segment.
    /// </returns>
    private IndexEntry? FindLastIndexEntryBeforeKey(TKey key, CancellationToken cancellationToken)
    {
        Debug.Assert(FirstIndexEntry.HasValue, "The first index entry must have a value.");
        Debug.Assert(LastIndexEntry.HasValue, "The last index entry must have a value.");
        Debug.Assert(Index.HasValue, "The index must have a value.");

        if (key.CompareTo(FirstIndexEntry.Value.Key) < 0 ||
            key.CompareTo(LastIndexEntry.Value.Key) > 0)
        {
            // The key being looked for cannot exist in this segment, as it is outside of the key range of the segment.
            return null;
        }

        return FindLastIndexEntryBeforeKey(key, Index.Value);
    }

    /// <summary>
    /// Finds the last <see cref="IndexEntry"/> (the entry with the highest key lower than the given
    /// <paramref name="key"/>) in the index.
    /// </summary>
    /// <param name="key">
    /// A <typeparamref name="TKey"/> value that represents the key that is to be found.
    /// </param>
    /// <param name="index">
    /// An <see cref="ArraySegment{T}"/> of <see cref="IndexEntry"/> that represents the subsection of the index in
    /// which to look for a matching entry.
    /// </param>
    /// <returns>
    /// An <see cref="IndexEntry"/> value representing the matching key from the index.
    /// </returns>
    /// <remarks>
    /// This effectively does a binary search to find the highest key less than or equal to the given
    /// <paramref name="key"/> on the specified <paramref name="index"/>.
    /// </remarks>
    private static IndexEntry FindLastIndexEntryBeforeKey(TKey key, ArraySegment<IndexEntry> index)
    {
        if (index.Count == 1)
        {
            return index[0];
        }

        int midPos = index.Count / 2;
        IndexEntry middle = index[midPos];

        int result = key.CompareTo(middle.Key);
        if (result == 0)
        {
            return middle;
        }
        else if (result < 0)
        {
            return FindLastIndexEntryBeforeKey(key, index.Slice(0, midPos));
        }
        else // if (result > 0)
        {
            return FindLastIndexEntryBeforeKey(key, index.Slice(midPos));
        }
    }

    /// <summary>
    /// Asynchronously tries to get the entry for the given <paramref name="key"/> from the segment's data file.
    /// </summary>
    /// <param name="startEntry">
    /// The <see cref="IndexEntry"/> value that defines where in the data file to start the search.
    /// </param>
    /// <param name="key">
    /// The <typeparamref name="TKey"/> value identifying the entry to get.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <see cref="StoreEntry{TKey, TValue}"/> that results in the entry if it is
    /// found, or <c>null</c> if the segment does not have an entry for the given <paramref name="key"/>.
    /// </returns>
    private async ValueTask<StoreEntry<TKey, TValue>?> GetEntryAsync(
        IndexEntry startEntry,
        TKey key,
        CancellationToken cancellationToken)
    {
        Debug.Assert(reader != null, "The reader must not be null.");
        Debug.Assert(Index.HasValue, "The index must not be null.");

        // The nextEntry should point at the next entry from the index provided such an entry exists, or null if we
        // start the search at the last entry of the segment - in that case, we want to read to the segment's data
        // file's end anyway.
        IndexEntry? nextEntry = GetNextIndexEntry(startEntry);
        long? readWindow = nextEntry?.Position - startEntry.Position;

        await using ReadContext context = new(
            await reader.OpenDataForReadAsync(startEntry.Position, readWindow, cancellationToken).ConfigureAwaitLib());

        StoreEntry<TKey, TValue>? entry = await SeekEntryAsync(context, key, cancellationToken).ConfigureAwaitLib();

        return entry;
    }

    /// <summary>
    /// Gets the <see cref="IndexEntry"/> that follows the given <paramref name="entry"/>
    /// if available.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="IndexEntry"/> for which to get the next entry in the index.
    /// </param>
    /// <returns>
    /// The next <see cref="IndexEntry"/> or <c>null</c> if there is no next index
    /// entry.
    /// </returns>
    private IndexEntry? GetNextIndexEntry(IndexEntry entry)
    {
        Debug.Assert(reader != null, "The reader must not be null.");
        Debug.Assert(Index.HasValue, "The index must not be null.");

        IndexEntry? nextEntry = entry.Id < Index.Value.Count - 1 ?
            Index.Value[entry.Id + 1] : null;

        return nextEntry;
    }

    /// <summary>
    /// Asynchronously seeks an entry for the given <paramref name="keyToMatch"/> in the segment's data file.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ReadContext"/> to use for seeking the entry.
    /// </param>
    /// <param name="keyToMatch">
    /// A <typeparamref name="TKey"/> value that represents the key of the entry to find.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that results in the entry if it is found, or <c>null</c> if the segment
    /// does not have an entry for the given <paramref name="keyToMatch"/>.
    /// </returns>
    private async ValueTask<StoreEntry<TKey, TValue>?> SeekEntryAsync(
        ReadContext context,
        TKey keyToMatch,
        CancellationToken cancellationToken)
    {
        EntryFlags flags = ReadEntryFlags(context);
        TKey key = await formatter.ReadKeyAsync(context.Stream, cancellationToken).ConfigureAwaitLib();

        int result = keyToMatch.CompareTo(key);
        // As long as the key of the entry we're looking for is greater than the key of the entry we've just read, we
        // can continue to read/skip entries in the data file.
        while (result > 0)
        {
            if (!flags.HasFlag(EntryFlags.Deleted))
            {
                // The entry isn't deleted, but we also don't care about its value, so skip the value.
                await formatter.SkipReadValueAsync(context.Stream, cancellationToken).ConfigureAwaitLib();
            }

            // Try to read the next entry's flags.
            try
            {
                flags = ReadEntryFlags(context);
            }
            catch (EndOfStreamException)
            {
                // There are no more entries left, so we know we couldn't find the entry for the key we cared about.
                return null;
            }
            key = await formatter.ReadKeyAsync(context.Stream, cancellationToken).ConfigureAwaitLib();
            result = keyToMatch.CompareTo(key);
        }

        // If the keys match, we've actually found ourselves the entry we're looking for. Read its value (if present)
        // and return a matching StoreEntry.
        if (result == 0)
        {
            if (!flags.HasFlag(EntryFlags.Deleted))
            {
                TValue value = await formatter.ReadValueAsync(context.Stream, cancellationToken).ConfigureAwaitLib();

                return new(key, value);
            }
            else
            {
                return StoreEntry<TKey, TValue>.Delete(key);
            }
        }

        // We've found an entry with a key that is greater than the key we care about, without having found a matching
        // key in the first place. So the entry cannot actually exist in this segment.
        return null;
    }

    /// <summary>
    /// Asynchronously writes an entry to the segment's data file.
    /// </summary>
    /// <param name="context">
    /// The <see cref="WriteContext"/> to write the entry to.
    /// </param>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to write.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    private async ValueTask WriteEntryAsync(
        WriteContext context,
        StoreEntry<TKey, TValue> entry,
        CancellationToken cancellationToken)
    {
        // Write to the data: first the flags, then the key, then the value (iff the entry isn't deleted).
        WriteEntryFlags(context, entry);
        await formatter.WriteKeyAsync(entry.Key, context.Stream, cancellationToken).ConfigureAwaitLib();

        if (!entry.IsDeleted)
        {
            await formatter.WriteValueAsync(entry.Value!, context.Stream, cancellationToken).ConfigureAwaitLib();
        }
    }

    /// <summary>
    /// Throws a <see cref="ObjectDisposedException"/> if this instance was already disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(nameof(Driver<TKey, TValue>));
        }
    }
}
