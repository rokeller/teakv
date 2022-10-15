using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="bool"/> values.
    /// </summary>
    public readonly struct BooleanFormatter : IFormatter<bool>
    {
        /// <inheritdoc/>>
        public ValueTask<bool> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            int result = source.ReadByte();
            if (result == -1)
            {
                throw new EndOfStreamException("Expected at least 1 more byte.");
            }

            return new(result != 0);
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
        public ValueTask WriteAsync(bool value, Stream destination, CancellationToken cancellationToken)
        {
            destination.WriteByte(value ? (byte)1 : (byte)0);

            return default;
        }
    }
}
