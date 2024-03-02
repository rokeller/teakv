using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="string"/> values.
    /// </summary>
    public readonly struct StringFormatter : IFormatter<string>
    {
        /// <summary>
        /// The mamximum number of bytes to allocate on the stack for the bytes of a string.
        /// </summary>
        private const int MaxStackAlloc = 1024;

        /// <summary>
        /// The <see cref="Encoding"/> to use for string serialization and deserialization.
        /// </summary>
        private readonly Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// Initializes a new instance of <see cref="StringFormatter"/>.
        /// </summary>
        public StringFormatter() { }

        /// <inheritdoc/>
        public ValueTask<string> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            source.Fill(buffer);

            int byteLength = BitConverter.ToInt32(buffer);
            if (byteLength <= MaxStackAlloc)
            {
                buffer = stackalloc byte[byteLength];
                source.Fill(buffer);

                return new(encoding.GetString(buffer));
            }
            else
            {
                return new(ReadWithPoolAsync(source, byteLength, cancellationToken));
            }
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            source.Fill(buffer);

            int remaining = BitConverter.ToInt32(buffer);
            buffer = stackalloc byte[Math.Min(remaining, MaxStackAlloc)];

            while (remaining > 0)
            {
                Span<byte> localBuffer = buffer.Slice(0, Math.Min(remaining, MaxStackAlloc));
                source.Fill(localBuffer);
                remaining -= localBuffer.Length;
            }

            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(string value, Stream destination, CancellationToken cancellationToken)
        {
            int byteLength = encoding.GetByteCount(value);

            Span<byte> buffer = stackalloc byte[sizeof(int)];
            bool successful = BitConverter.TryWriteBytes(buffer, byteLength);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");

            destination.Write(buffer);

            if (byteLength <= MaxStackAlloc)
            {
                buffer = stackalloc byte[byteLength];
                encoding.GetBytes(value, buffer);
                destination.Write(buffer);

                return default;
            }
            else
            {
                byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(byteLength);
                try
                {
                    encoding.GetBytes(value, 0, value.Length, byteBuffer, 0);
                    return new ValueTask(destination.WriteAsync(byteBuffer, 0, byteLength));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(byteBuffer);
                }
            }
        }

        /// <summary>
        /// Reads a string with the given <paramref name="length"/> from the <paramref name="source"/> and stores the
        /// bytes in a shared byte array pool.
        /// </summary>
        /// <param name="source">
        /// The <see cref="Stream"/> from which to read.
        /// </param>
        /// <param name="length">
        /// The length of the string to read, in bytes.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that tracks cancellation of the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which results in the string that was read on success.
        /// </returns>
        private async Task<string> ReadWithPoolAsync(Stream source, int length, CancellationToken cancellationToken)
        {
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(length);
            Memory<byte> memoryBuffer = new Memory<byte>(byteBuffer, 0, length);
            try
            {
                await source.FillAsync(memoryBuffer, cancellationToken).ConfigureAwaitLib();

                return encoding.GetString(memoryBuffer.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }
    }
}
