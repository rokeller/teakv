using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines the contract used by writers of segments.
/// </summary>
public interface ISegmentWriter : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Opens the index file of the segment for writing.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that results in a <see cref="Stream"/> representing the index stream when it succeeds.
    /// </returns>
    ValueTask<Stream> OpenIndexForWriteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens the data file of the segment for writing.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that results in a <see cref="Stream"/> representing the data stream when it succeeds.
    /// </returns>
    ValueTask<Stream> OpenDataForWriteAsync(CancellationToken cancellationToken);
}
