using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="double"/> values.
    /// </summary>
    public readonly struct DoubleFormatter : IFormatter<double>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DoubleFormatter"/>.
        /// </summary>
        public DoubleFormatter() { }

        /// <inheritdoc/>
        public ValueTask<double> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(double)];
            source.Fill(buffer, buffer.Length);
            return new(BitConverter.ToDouble(buffer, 0));
#else
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            source.Fill(buffer);
            return new(BitConverter.ToDouble(buffer));
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(sizeof(double));
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(double value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(value);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            bool successful = BitConverter.TryWriteBytes(buffer, value);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
