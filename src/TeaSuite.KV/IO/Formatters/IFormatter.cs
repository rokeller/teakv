using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

/// <summary>
/// Defines the contract for a formatter that can read, write and skip data of a specific type on a Stream.
/// </summary>
/// <typeparam name="T">
/// The type supported by the formatter
/// </typeparam>
public interface IFormatter<T>
{
    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the given source stream.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> from which to read the value.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <typeparamref name="T"/> that tracks availabiliby of the read value.
    /// </returns>
    ValueTask<T> ReadAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Skips a value of type <typeparamref name="T"/> from the given source stream.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> in which to skip the value.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a value of type <typeparamref name="T"/> to the given destination stream.
    /// </summary>
    /// <param name="value">
    /// The value of <typeparamref name="T"/> to write to the stream.
    /// </param>
    /// <param name="destination">
    /// The <see cref="Stream"/> to which to write the value.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    ValueTask WriteAsync(T value, Stream destination, CancellationToken cancellationToken);
}
