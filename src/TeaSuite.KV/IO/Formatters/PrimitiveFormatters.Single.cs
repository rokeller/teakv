using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="float"/> values.
    /// </summary>
    public readonly struct SingleFormatter : IFormatter<float>
    {
        /// <inheritdoc/>>
        public ValueTask<float> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            source.Fill(buffer);

            return new(BitConverter.ToSingle(buffer));
        }

        /// <inheritdoc/>>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(sizeof(float), SeekOrigin.Current);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[sizeof(float)];
                source.Fill(buffer);
            }

            return default;
        }

        /// <inheritdoc/>>
        public ValueTask WriteAsync(float value, Stream destination, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            return default;
        }
    }
}
