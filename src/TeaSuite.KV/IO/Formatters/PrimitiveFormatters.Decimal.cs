using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="decimal"/> values.
    /// </summary>
    public readonly struct DecimalFormatter : IFormatter<decimal>
    {
        /// <inheritdoc/>>
        public ValueTask<decimal> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[4 * sizeof(int)];
            source.Fill(buffer);

            int[] bits = new int[4];
            bits[0] = BitConverter.ToInt32(buffer[0..sizeof(int)]);
            bits[1] = BitConverter.ToInt32(buffer[sizeof(int)..(2 * sizeof(int))]);
            bits[2] = BitConverter.ToInt32(buffer[(2 * sizeof(int))..(3 * sizeof(int))]);
            bits[3] = BitConverter.ToInt32(buffer[(3 * sizeof(int))..]);

            return new(new Decimal(bits));
        }

        /// <inheritdoc/>>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            if (source.CanSeek)
            {
                source.Seek(4 * sizeof(int), SeekOrigin.Current);
            }
            else
            {
                Span<byte> buffer = stackalloc byte[4 * sizeof(int)];
                source.Fill(buffer);
            }

            return default;
        }

        /// <inheritdoc/>>
        public ValueTask WriteAsync(decimal value, Stream destination, CancellationToken cancellationToken)
        {
            int[] bits = Decimal.GetBits(value);
            Debug.Assert(bits.Length == 4, "There must be for 32-bit integers.");

            Span<byte> buffer = stackalloc byte[4 * sizeof(int)];
            bool successful = BitConverter.TryWriteBytes(buffer[0..sizeof(int)], bits[0]);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            successful = BitConverter.TryWriteBytes(buffer[sizeof(int)..(2 * sizeof(int))], bits[1]);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            successful = BitConverter.TryWriteBytes(buffer[(2 * sizeof(int))..(3 * sizeof(int))], bits[2]);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            successful = BitConverter.TryWriteBytes(buffer[(3 * sizeof(int))..], bits[3]);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            return default;
        }
    }
}
