using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="int"/> values.
    /// </summary>
    public readonly struct Int32Formatter : IFormatter<int>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Int32Formatter"/>.
        /// </summary>
        public Int32Formatter() { }

        /// <inheritdoc/>
        public ValueTask<int> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            source.Fill(buffer);

            return new(BitConverter.ToInt32(buffer));
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(sizeof(int), SeekOrigin.Current);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[sizeof(int)];
                source.Fill(buffer);
            }

            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(int value, Stream destination, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            return default;
        }
    }
}
