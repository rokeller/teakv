using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="ulong"/> values.
    /// </summary>
    public readonly struct UInt64Formatter : IFormatter<ulong>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UInt64Formatter"/>.
        /// </summary>
        public UInt64Formatter() { }

        /// <inheritdoc/>
        public ValueTask<ulong> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(ulong)];
            source.Fill(buffer, sizeof(ulong));
            return new(BitConverter.ToUInt64(buffer, 0));
#else
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            source.Fill(buffer);
            return new(BitConverter.ToUInt64(buffer));
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(ulong));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(ulong value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
