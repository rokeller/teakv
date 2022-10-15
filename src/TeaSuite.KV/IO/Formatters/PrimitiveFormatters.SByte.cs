using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="sbyte"/> values.
    /// </summary>
    public readonly struct SByteFormatter : IFormatter<sbyte>
    {
        /// <inheritdoc/>>
        public ValueTask<sbyte> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            int result = source.ReadByte();
            if (result == -1)
            {
                throw new EndOfStreamException("Expected at least 1 more byte.");
            }

            return new(unchecked((sbyte)result));
        }

        /// <inheritdoc/>>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            int result = source.ReadByte();
            if (result == -1)
            {
                throw new EndOfStreamException("Expected at least 1 more byte.");
            }

            return default;
        }

        /// <inheritdoc/>>
        public ValueTask WriteAsync(sbyte value, Stream destination, CancellationToken cancellationToken)
        {
            destination.WriteByte(unchecked((byte)value));

            return default;
        }
    }
}
