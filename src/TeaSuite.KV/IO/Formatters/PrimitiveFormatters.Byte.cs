using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="byte"/> values.
    /// </summary>
    public readonly struct ByteFormatter : IFormatter<byte>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ByteFormatter"/>.
        /// </summary>
        public ByteFormatter() { }

        /// <inheritdoc/>
        public ValueTask<byte> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            int result = source.ReadByte();
            if (result == -1)
            {
                throw new EndOfStreamException("Expected at least 1 more byte.");
            }

            return new((byte)result);
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            int result = source.ReadByte();
            if (result == -1)
            {
                throw new EndOfStreamException("Expected at least 1 more byte.");
            }

            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(byte value, Stream destination, CancellationToken cancellationToken)
        {
            destination.WriteByte(value);

            return default;
        }
    }
}
