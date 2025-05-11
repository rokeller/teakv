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
        /// <summary>
        /// Initializes a new instance of <see cref="Int16Formatter"/>.
        /// </summary>
        public Int16Formatter() { }

        /// <inheritdoc/>
        public ValueTask<short> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(short)];
            source.Fill(buffer, buffer.Length);
            return new(BitConverter.ToInt16(buffer, 0));
#else
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            source.Fill(buffer);
            return new(BitConverter.ToInt16(buffer));
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(short));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(short value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
