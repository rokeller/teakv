using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines the contract used by readers of segments.
/// </summary>
public interface ISegmentReader : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Opens the index file of the segment for reading.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that results in a <see cref="Stream"/> representing the index stream when it succeeds.
    /// </returns>
    ValueTask<Stream> OpenIndexForReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens the data file of the segment for reading.
    /// </summary>
    /// <param name="position">
    /// A <see cref="long"/> value that indicates the position (in bytes) at which to position the read stream in the
    /// data file.
    /// </param>
    /// <param name="readWindow">
    /// A <see cref="long?"/> value that indicates how long the read window in the data stream is supposed to be. If
    /// <c>null</c> is provided, the read window should be extended to the end of the file.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that results in a <see cref="Stream"/> representing the data stream at the requested
    /// <paramref name="position"/> when it succeeds.
    /// </returns>
    ValueTask<Stream> OpenDataForReadAsync(long position, long? readWindow, CancellationToken cancellationToken);
}
