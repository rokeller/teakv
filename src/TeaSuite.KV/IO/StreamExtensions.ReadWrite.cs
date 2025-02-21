using System;
using System.Diagnostics;
using System.IO;

namespace TeaSuite.KV.IO;

static partial class StreamExtensions
{
    /// <summary>
    /// Reads a <see cref="long"/> value from the given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> to read the value from.
    /// </param>
    /// <param name="value">
    /// On return, holds the value that was read.
    /// </param>
    public static void Read(this Stream source, out long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        source.Fill(buffer);
        value = BitConverter.ToInt64(buffer);
    }

    /// <summary>
    /// Writes a <see cref="long"/> value to the given <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// The <see cref="Stream"/> to write the value to.
    /// </param>
    /// <param name="value">
    /// The value to write.
    /// </param>
    public static void Write(this Stream destination, long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        bool successful = BitConverter.TryWriteBytes(buffer, value);
        Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
        destination.Write(buffer);
    }

    /// <summary>
    /// Reads a <see cref="uint"/> value from the given <paramref name="source"/>.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> to read the value from.
    /// </param>
    /// <param name="value">
    /// On return, holds the value that was read.
    /// </param>
    public static void Read(this Stream source, out uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        source.Fill(buffer);
        value = BitConverter.ToUInt32(buffer);
    }

    /// <summary>
    /// Writes a <see cref="uint"/> value to the given <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">
    /// The <see cref="Stream"/> to write the value to.
    /// </param>
    /// <param name="value">
    /// The value to write.
    /// </param>
    public static void Write(this Stream destination, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        bool successful = BitConverter.TryWriteBytes(buffer, value);
        Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
        destination.Write(buffer);
    }
}
