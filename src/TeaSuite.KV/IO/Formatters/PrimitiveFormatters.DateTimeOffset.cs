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
        /// <inheritdoc/>>
        public ValueTask<DateTimeOffset> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[2 * sizeof(long)];
            source.Fill(buffer);

            long ticks = BitConverter.ToInt64(buffer[0..sizeof(long)]);
            long offsetTicks = BitConverter.ToInt64(buffer[sizeof(long)..]);

            return new(new DateTimeOffset(ticks, new TimeSpan(offsetTicks)));
        }

        /// <inheritdoc/>>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(2 * sizeof(long), SeekOrigin.Current);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[2 * sizeof(long)];
                source.Fill(buffer);
            }

            return default;
        }

        /// <inheritdoc/>>
        public ValueTask WriteAsync(DateTimeOffset value, Stream destination, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[2 * sizeof(long)];
            bool successful = BitConverter.TryWriteBytes(buffer[0..sizeof(long)], value.Ticks);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            successful = BitConverter.TryWriteBytes(buffer[sizeof(long)..], value.Offset.Ticks);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            return default;
        }
    }
}
