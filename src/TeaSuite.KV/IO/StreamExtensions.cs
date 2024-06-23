using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines extensions for <see cref="Stream"/> objects.
/// </summary>
public static partial class StreamExtensions
{
    /// <summary>
    /// Fills the given <paramref name="buffer"/> with data from the <see cref="Stream"/>
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to read from to fill the buffer.
    /// </param>
    /// <param name="buffer">
    /// A <see cref="Span{T}"/> of <see cref="byte"/> values to fill with data from the stream.
    /// </param>
    /// <exception cref="EndOfStreamException">
    /// Thrown when the stream ends before the <paramref name="buffer"/> could be filled.
    /// </exception>
    public static void Fill(this Stream stream, Span<byte> buffer)
    {
        Span<byte> remainder = buffer;

        while (!remainder.IsEmpty)
        {
            int read = stream.Read(remainder);
            if (read == 0)
            {
                // We're at the end of the stream, yet we were supposed to read more.
                throw new EndOfStreamException($"Expected at least {remainder.Length} more bytes.");
            }

            remainder = remainder.Slice(read);
        }
    }

    /// <summary>
    /// Asynchronoulsy fills the given <paramref name="buffer"/> with data from the <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> to read from to fill the buffer.
    /// </param>
    /// <param name="buffer">
    /// A <see cref="Memory{T}"/> of <see cref="byte"/> value to fill with data from the stream.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that tracks completion of the operation.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown when the stream ends before the <paramref name="buffer"/> could be filled.
    /// </exception>
    public static async Task FillAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        Memory<byte> remainder = buffer;

        while (!remainder.IsEmpty)
        {
            int read = await stream.ReadAsync(remainder, cancellationToken).ConfigureAwaitLib();
            if (read == 0)
            {
                // We're at the end of the stream, yet we were supposed to read more.
                throw new EndOfStreamException($"Expected at least {remainder.Length} more bytes.");
            }

            remainder = remainder.Slice(read);
        }
    }
}
