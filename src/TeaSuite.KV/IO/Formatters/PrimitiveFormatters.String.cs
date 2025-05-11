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
        /// The mamximum number of bytes to allocate on the stack for the bytes
        /// of a string.
        /// </summary>
        private const int MaxStackAlloc = 1024;

        /// <summary>
        /// The <see cref="Encoding"/> to use for string serialization and
        /// deserialization.
        /// </summary>
        private readonly Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// Initializes a new instance of <see cref="StringFormatter"/>.
        /// </summary>
        public StringFormatter() { }

        /// <inheritdoc/>
        public ValueTask<string> ReadAsync(
            Stream source,
            CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(int)];
            source.Fill(buffer, buffer.Length);
            int byteLength = BitConverter.ToInt32(buffer, 0);
            return new(ReadWithPoolAsync(source, byteLength, cancellationToken));
#else
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
                return new(ReadWithPoolAsync(source, byteLength, cancellationToken));
            }
#endif
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(
            Stream source,
            CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[sizeof(int)];
            source.Fill(buffer, buffer.Length);
            int remaining = BitConverter.ToInt32(buffer, 0);
#else
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            source.Fill(buffer);
            int remaining = BitConverter.ToInt32(buffer);
#endif
            source.Skip(remaining);
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(
            string value,
            Stream destination,
            CancellationToken cancellationToken)
        {
            int byteLength = encoding.GetByteCount(value);
#if NETSTANDARD
            byte[] buffer = BitConverter.GetBytes(byteLength);
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            bool successful = BitConverter.TryWriteBytes(buffer, byteLength);
            Debug.Assert(successful,
                "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif

#if NETSTANDARD
            return WriteWithPoolAsync(destination, value, byteLength);
#else
            if (byteLength <= MaxStackAlloc)
            {
                buffer = stackalloc byte[byteLength];
                encoding.GetBytes(value, buffer);
                destination.Write(buffer);

                return default;
            }
            else
            {
                return WriteWithPoolAsync(destination, value, byteLength);
            }
#endif
        }

        /// <summary>
        /// Reads a string with the given <paramref name="length"/> from the
        /// <paramref name="source"/> and stores the
        /// bytes in a shared byte array pool.
        /// </summary>
        /// <param name="source">
        /// The <see cref="Stream"/> from which to read.
        /// </param>
        /// <param name="length">
        /// The length of the string to read, in bytes.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that tracks cancellation of
        /// the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> which results in the string that was read on
        /// success.
        /// </returns>
        private async Task<string> ReadWithPoolAsync(
            Stream source,
            int length,
            CancellationToken cancellationToken)
        {
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                await source.FillAsync(byteBuffer, length, cancellationToken).ConfigureAwaitLib();
                return encoding.GetString(byteBuffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }

        private ValueTask WriteWithPoolAsync(
            Stream destination,
            string value,
            int byteLength)
        {
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(byteLength);
            try
            {
                encoding.GetBytes(value, 0, value.Length, byteBuffer, 0);
                return new(destination.WriteAsync(byteBuffer, 0, byteLength));
            try
            {
                await source.FillAsync(byteBuffer, length, cancellationToken).ConfigureAwaitLib();
                return encoding.GetString(byteBuffer, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }

        private ValueTask WriteWithPoolAsync(
            Stream destination,
            string value,
            int byteLength)
        {
            byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(byteLength);
            try
            {
                encoding.GetBytes(value, 0, value.Length, byteBuffer, 0);
                return new(destination.WriteAsync(byteBuffer, 0, byteLength));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }
    }
}
