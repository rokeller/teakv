using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

/// <summary>
/// Defines the contract for a formatter of entries in a key/value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used with entries.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used with entries.
/// </typeparam>
public interface IEntryFormatter<TKey, TValue>
{
    /// <summary>
    /// Reads a key from the given source stream.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> from which to read the key.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <typeparamref name="TKey"/> that tracks availabiliby of the read key.
    /// </returns>
    ValueTask<TKey> ReadKeyAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a value from the given source stream.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> from which to read the value.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <typeparamref name="TValue"/> that tracks availabiliby of the read value.
    /// </returns>
    ValueTask<TValue> ReadValueAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Skips reading a value on the given source stream.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Stream"/> on which to skip the value.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that tracks if the operation should be cancelled.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    ValueTask SkipReadValueAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a key to the given destination stream.
    /// </summary>
    /// <param name="key">
    /// The value of <typeparamref name="TKey"/> to write to the stream.
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
    ValueTask WriteKeyAsync(TKey key, Stream destination, CancellationToken cancellationToken);

    /// <summary>
    /// Writes a value to the given destination stream.
    /// </summary>
    /// <param name="value">
    /// The value of <typeparamref name="TValue"/> to write to the stream.
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
    ValueTask WriteValueAsync(TValue value, Stream destination, CancellationToken cancellationToken);
}
