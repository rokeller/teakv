using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="DateTimeOffset"/> values.
    /// </summary>
    public readonly struct DateTimeOffsetFormatter : IFormatter<DateTimeOffset>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DateTimeOffsetFormatter"/>.
        /// </summary>
        public DateTimeOffsetFormatter() { }

        /// <inheritdoc/>
        public ValueTask<DateTimeOffset> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[2 * sizeof(long)];
            source.Fill(buffer, buffer.Length);
            long ticks = BitConverter.ToInt64(buffer, 0);
            long offsetTicks = BitConverter.ToInt64(buffer, sizeof(long));
#else
            Span<byte> buffer = stackalloc byte[2 * sizeof(long)];
            source.Fill(buffer);
            long ticks = BitConverter.ToInt64(buffer[0..sizeof(long)]);
            long offsetTicks = BitConverter.ToInt64(buffer[sizeof(long)..]);
#endif
            return new(new DateTimeOffset(ticks, new TimeSpan(offsetTicks)));
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(2 * sizeof(long));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(DateTimeOffset value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value.Ticks);
            destination.Write(buffer, 0, buffer.Length);
            buffer = BitConverter.GetBytes(value.Offset.Ticks);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[2 * sizeof(long)];
            bool successful = BitConverter.TryWriteBytes(buffer[0..sizeof(long)], value.Ticks);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            successful = BitConverter.TryWriteBytes(buffer[sizeof(long)..], value.Offset.Ticks);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
