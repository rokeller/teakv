using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="DateTime"/> values.
    /// </summary>
    public readonly struct DateTimeFormatter : IFormatter<DateTime>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DateTimeFormatter"/>.
        /// </summary>
        public DateTimeFormatter() { }

        /// <inheritdoc/>
        public ValueTask<DateTime> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(long)];
            source.Fill(buffer, buffer.Length);
            long ticks = BitConverter.ToInt64(buffer, 0);
#else
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            source.Fill(buffer);
            long ticks = BitConverter.ToInt64(buffer);
#endif
            return new(new DateTime(ticks, DateTimeKind.Utc));
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(long));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(DateTime value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value.ToUniversalTime().Ticks);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            bool successful = BitConverter.TryWriteBytes(buffer, value.ToUniversalTime().Ticks);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
