using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV.IO;

/// <summary>
/// Implements <see cref="ISegmentManager{TKey, TValue}"/> for memory mapped files, based on
/// <see cref="FileSegmentManager{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used in the segments.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used in the segments.
/// </typeparam>
/// <remarks>
/// Only the reading of segment data files is done through memory mapped files, as the reading of
/// index files is a one-time operation when the segment is loaded. Writing of segment files (both
/// data and index) is a relatively short-lived one-time operation too, so wouldn't benefit much
/// from using memory mapped files.
/// </remarks>
public sealed partial class MemoryMappedFileSegmentManager<TKey, TValue> :
    FileSegmentManager<TKey, TValue>
    where TKey : IComparable<TKey>
{
    /// <inheritdoc/>
    public MemoryMappedFileSegmentManager(
        ILogger<MemoryMappedFileSegmentManager<TKey, TValue>> logger,
        ILoggerFactory loggerFactory,
        IEntryFormatter<TKey, TValue> entryFormatter,
        IOptionsMonitor<FileSegmentsOptions> fileSegmentsOptions
        )
        : base(logger, loggerFactory, entryFormatter, fileSegmentsOptions)
    { }

    /// <inheritdoc/>
    protected override ISegmentReader CreateSegmentReader(string indexFilePath, string dataFilePath)
    {
        return new SegmentReader(Logger, indexFilePath, dataFilePath);
    }

    /// <summary>
    /// Implements <see cref="ISegmentReader"/> for memory mapped files.
    /// </summary>
    private readonly struct SegmentReader : ISegmentReader
    {
        private readonly ILogger logger;
        private readonly string indexFilePath;
        private readonly string dataFilePath;
        private readonly MemoryMappedFile dataFile;

        public SegmentReader(ILogger logger, string indexFilePath, string dataFilePath)
        {
            this.logger = logger;
            this.indexFilePath = indexFilePath;
            this.dataFilePath = dataFilePath;

            logger.LogInformation("Creating memory mapped file for '{path}'.", dataFilePath);
            dataFile = MemoryMappedFile.CreateFromFile(dataFilePath, FileMode.Open, null, 0);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            dataFile.Dispose();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            dataFile.Dispose();

            return default;
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenDataForReadAsync(
            long position,
            long? readWindow,
            CancellationToken cancellationToken)
        {
            return new(
                dataFile.CreateViewStream(
                    position, readWindow ?? 0, MemoryMappedFileAccess.Read));
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenIndexForReadAsync(CancellationToken cancellationToken)
        {
            return new(
                File.Open(
                    indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
    }
}
