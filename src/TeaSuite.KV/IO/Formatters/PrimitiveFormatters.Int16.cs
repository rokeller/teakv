using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="short"/> values.
    /// </summary>
    public readonly struct Int16Formatter : IFormatter<short>
    {
        /// <inheritdoc/>>
        public ValueTask<short> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            source.Fill(buffer);

            return new(BitConverter.ToInt16(buffer));
        }

        /// <inheritdoc/>>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(sizeof(short), SeekOrigin.Current);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[sizeof(short)];
                source.Fill(buffer);
            }

            return default;
        }

        /// <inheritdoc/>>
        public ValueTask WriteAsync(short value, Stream destination, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            return default;
        }
    }
}
