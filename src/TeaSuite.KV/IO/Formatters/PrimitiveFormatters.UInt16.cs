using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="ushort"/> values.
    /// </summary>
    public readonly struct UInt16Formatter : IFormatter<ushort>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UInt16Formatter"/>.
        /// </summary>
        public UInt16Formatter() { }

        /// <inheritdoc/>
        public ValueTask<ushort> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            source.Fill(buffer);
            return new(BitConverter.ToUInt16(buffer));
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(ushort));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(ushort value, Stream destination, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
            return default;
        }
    }
}
