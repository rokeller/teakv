using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="uint"/> values.
    /// </summary>
    public readonly struct UInt32Formatter : IFormatter<uint>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UInt32Formatter"/>.
        /// </summary>
        public UInt32Formatter() { }

        /// <inheritdoc/>
        public ValueTask<uint> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(uint)];
            source.Fill(buffer, buffer.Length);
            return new(BitConverter.ToUInt32(buffer, 0));
#else
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            source.Fill(buffer);
            return new(BitConverter.ToUInt32(buffer));
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(uint));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(uint value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
