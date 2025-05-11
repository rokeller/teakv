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
        /// <summary>
        /// Initializes a new instance of <see cref="SingleFormatter"/>.
        /// </summary>
        public SingleFormatter() { }

        /// <inheritdoc/>
        public ValueTask<float> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(float)];
            source.Fill(buffer, buffer.Length);
            return new(BitConverter.ToSingle(buffer, 0));
#else
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            source.Fill(buffer);
            return new(BitConverter.ToSingle(buffer));
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(float));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(float value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
