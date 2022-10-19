using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Reads segment metadata from the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ReadContext"/> to read from.
    /// </param>
    /// <returns>
    /// An instance of <see cref="SegmentMetadata"/> that represents the segment's metadata.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the stream was written with a different byte order, or when the segment version is not supported.
    /// </exception>
    private static SegmentMetadata ReadSegmentMetadata(ReadContext context)
    {
        Read(out uint rawFlags, context.Stream);
        SegmentFlags flags = (SegmentFlags)rawFlags;

        if (BitConverter.IsLittleEndian ^ flags.HasFlag(SegmentFlags.LittleEndian))
        {
            throw new NotSupportedException(
                $"The machine is {(BitConverter.IsLittleEndian ? "little" : "big")} endian but the segment is not.");
        }

        Read(out uint version, context.Stream);

        if (version < 1 || version > SegmentMetadata.CurrentVersion)
        {
            throw new NotSupportedException($"Segments of version {version} are not supported.");
        }

        Read(out long ticks, context.Stream);

        return new SegmentMetadata(flags, version, new DateTime(ticks, DateTimeKind.Utc));
    }

    /// <summary>
    /// Writes the given <paramref name="metadata"/> to the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="WriteContext"/> to write to.
    /// </param>
    /// <param name="metadata">
    /// The <see cref="SegmentMetadata"/> to write.
    /// </param>
    private static void WriteSegmentMetadata(WriteContext context, SegmentMetadata metadata)
    {
        Write((uint)metadata.Flags, context.Stream);
        Write(metadata.Version, context.Stream);
        Write(metadata.Timestamp.Ticks, context.Stream);
    }

    /// <summary>
    /// Asynchronously reads the next <see cref="IndexEntry"/> from the given <paramref name="context"/>.
    /// </summary>
    /// <param name="indexId">
    /// The ID (position) of the entry to be read from the index.
    /// </param>
    /// <param name="context">
    /// The <see cref="ReadContext"/> to read from.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that results in a <see cref="IndexEntry?"/> when it completes. The result
    /// will be null if there are no more entries left to be read from the index.
    /// </returns>
    private async ValueTask<IndexEntry?> ReadIndexEntryAsync(
        int indexId,
        ReadContext context,
        CancellationToken cancellationToken)
    {
        TKey key;
        try
        {
            key = await formatter.ReadKeyAsync(context.Stream, cancellationToken).ConfigureAwaitLib();
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        Read(out long position, context.Stream);

        return new IndexEntry(indexId, key, position);
    }

    /// <summary>
    /// Asynchronously writes the given <paramref name="entry"/> to the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="WriteContext"/> to write the entry to.
    /// </param>
    /// <param name="entry">
    /// The <see cref="IndexEntry"/> to write.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> value that tracks completion of the operation.
    /// </returns>
    private async ValueTask WriteIndexEntryAsync(WriteContext context, IndexEntry entry, CancellationToken cancellationToken)
    {
        await formatter.WriteKeyAsync(entry.Key, context.Stream, cancellationToken).ConfigureAwaitLib();
        Write(entry.Position, context.Stream);
    }

    /// <summary>
    /// Reads an entry's flags from the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ReadContext"/> to read from.
    /// </param>
    /// <returns>
    /// A <see cref="EntryFlags"/> value representing the entry's flags.
    /// </returns>
    private static EntryFlags ReadEntryFlags(ReadContext context)
    {
        uint rawFlags;
        Read(out rawFlags, context.Stream);
        return (EntryFlags)rawFlags;
    }

    /// <summary>
    /// Writes the given <paramref name="entry"/>'s flags to the specified <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="WriteContext"/> to write to.
    /// </param>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> for which to write the flags.
    /// </param>
    private static void WriteEntryFlags(WriteContext context, StoreEntry<TKey, TValue> entry)
    {
        EntryFlags flags = entry.IsDeleted ? EntryFlags.Deleted : EntryFlags.None;
        Write((uint)flags, context.Stream);
    }

    /// <summary>
    /// Reads a <see cref="long"/> value from the given <paramref name="source"/>.
    /// </summary>
    /// <param name="value">
    /// On return, holds the value that was read.
    /// </param>
    /// <param name="source">
    /// The <see cref="Stream"/> to read the value from.
    /// </param>
    private static void Read(out long value, Stream source)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        source.Fill(buffer);
        value = BitConverter.ToInt64(buffer);
    }

    /// <summary>
    /// Writes a <see cref="long"/> value to the given <paramref name="destination"/>.
    /// </summary>
    /// <param name="value">
    /// The value to write.
    /// </param>
    /// <param name="destination">
    /// The <see cref="Stream"/> to write the value to.
    /// </param>
    private static void Write(long value, Stream destination)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        bool successful = BitConverter.TryWriteBytes(buffer, value);
        Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
        destination.Write(buffer);
    }

    /// <summary>
    /// Reads a <see cref="uint"/> value from the given <paramref name="source"/>.
    /// </summary>
    /// <param name="value">
    /// On return, holds the value that was read.
    /// </param>
    /// <param name="source">
    /// The <see cref="Stream"/> to read the value from.
    /// </param>
    private static void Read(out uint value, Stream source)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        source.Fill(buffer);
        value = BitConverter.ToUInt32(buffer);
    }

    /// <summary>
    /// Writes a <see cref="uint"/> value to the given <paramref name="destination"/>.
    /// </summary>
    /// <param name="value">
    /// The value to write.
    /// </param>
    /// <param name="destination">
    /// The <see cref="Stream"/> to write the value to.
    /// </param>
    private static void Write(uint value, Stream destination)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        bool successful = BitConverter.TryWriteBytes(buffer, value);
        Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
        destination.Write(buffer);
    }
}
