using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV.IO;

/// <summary>
/// Implements <see cref="ISegmentManager{TKey, TValue}"/> using segments
/// persisted to files.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used in the segments.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used in the segments.
/// </typeparam>
public partial class FileSegmentManager<TKey, TValue>
    : ISegmentManager<TKey, TValue>
    where TKey : IComparable<TKey>
{
    #region Consts

    /// <summary>
    /// The file extension for index files.
    /// </summary>
    private const string IndexExtension = ".index";

    /// <summary>
    /// The file extension for data files.
    /// </summary>
    private const string DataExtension = ".data";

    /// <summary>
    /// The file name prefix for segment files.
    /// </summary>
    private const string SegmentFilePrefix = "segment_";

    /// <summary>
    /// The format string for numbered segment files.
    /// </summary>
    private const string SegmentsFileNamingConvention = SegmentFilePrefix + "{0:d12}";

    #endregion

    #region Fields

    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<FileSegmentManager<TKey, TValue>> logger;
    private readonly IEntryFormatter<TKey, TValue> entryFormatter;
    private readonly DirectoryInfo segmentsDir;

    #endregion

    /// <summary>
    /// Initializes a new instance of <see cref="FileSegmentManager{TKey, TValue}"/>.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger{TCategoryName}"/> to use.
    /// </param>
    /// <param name="loggerFactory">
    /// The <see cref="ILoggerFactory"/> to use.
    /// </param>
    /// <param name="entryFormatter">
    /// The <see cref="IEntryFormatter{TKey, TValue}"/> to use.
    /// </param>
    /// <param name="fileSegmentsOptions">
    /// An <see cref="IOptionsMonitor{TOptions}"/> of
    /// <see cref="FileSegmentsOptions"/> holding settings for the file segments.
    /// </param>
    public FileSegmentManager(
        ILogger<FileSegmentManager<TKey, TValue>> logger,
        ILoggerFactory loggerFactory,
        IEntryFormatter<TKey, TValue> entryFormatter,
        IOptionsMonitor<FileSegmentsOptions> fileSegmentsOptions)
    {
        this.logger = logger;
        this.loggerFactory = loggerFactory;
        this.entryFormatter = entryFormatter;

        FileSegmentsOptions options = fileSegmentsOptions
            .GetForStore<FileSegmentsOptions, TKey, TValue>();
        segmentsDir = Directory.CreateDirectory(options.SegmentsDirectoryPath);
    }

    /// <summary>
    /// The <see cref="ILogger"/>> to use for logging.
    /// </summary>
    protected ILogger Logger => logger;

    /// <summary>
    /// The <see cref="DirectoryInfo"/> holding information about the directory
    /// in which the segments are stored.
    /// </summary>
    protected DirectoryInfo SegmentsDir => segmentsDir;

    /// <inheritdoc/>
    public virtual Segment<TKey, TValue> CreateNewSegment(long segmentId)
    {
        (string indexFilePath, string dataFilePath) = GetFilePaths(segmentId);

        return new(
            segmentId,
            new(
                loggerFactory.CreateLogger<Driver<TKey, TValue>>(),
                CreateSegmentWriter(indexFilePath, dataFilePath),
                entryFormatter));
    }

    /// <inheritdoc/>
    public virtual ValueTask DeleteSegmentAsync(
        long segmentId,
        CancellationToken cancellationToken)
    {
        (string indexFilePath, string dataFilePath) = GetFilePaths(segmentId);

        File.Delete(indexFilePath);
        File.Delete(dataFilePath);

        return default;
    }

    /// <inheritdoc/>
    public virtual Segment<TKey, TValue> MakeReadOnly(Segment<TKey, TValue> segment)
    {
        return CreateReadOnlySegment(segment.Id);
    }

    /// <inheritdoc/>
    public IEnumerable<Segment<TKey, TValue>> DiscoverSegments()
    {
        // Let's enumerate just the index files we find in the directory.
        foreach (FileInfo indexFile in segmentsDir.EnumerateFiles("*" + IndexExtension))
        {
            string indexFilePath = indexFile.FullName;
            string dataFilePath = Path.ChangeExtension(indexFilePath, DataExtension);

            // But let's only return segments for which we also find a data file.
            if (File.Exists(dataFilePath))
            {
#if NETSTANDARD2_0
                string baseFileName = Path
                    .GetFileNameWithoutExtension(dataFilePath);
                long segmentId = Int64.Parse(
                    baseFileName.Substring(SegmentFilePrefix.Length));
#else
                ReadOnlySpan<char> baseFileName = Path
                    .GetFileNameWithoutExtension(dataFilePath);
                long segmentId = Int64.Parse(
                    baseFileName.Slice(SegmentFilePrefix.Length));
#endif

                yield return CreateReadOnlySegment(
                    segmentId, indexFilePath, dataFilePath);
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="ISegmentReader"/> for the index and data files
    /// from the specified <paramref name="indexFilePath"/> and
    /// <paramref name="dataFilePath"/>.
    /// </summary>
    /// <param name="indexFilePath">
    /// The path to the index file to create the <see cref="ISegmentReader"/> for.
    /// </param>
    /// <param name="dataFilePath">
    /// The path to the data file to create the <see cref="ISegmentReader"/> for.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ISegmentReader"/> that can be used to read the
    /// segment.
    /// </returns>
    protected virtual ISegmentReader CreateSegmentReader(
        string indexFilePath,
        string dataFilePath)
    {
        return new SegmentReader(indexFilePath, dataFilePath);
    }

    /// <summary>
    /// Creates a new <see cref="ISegmentWriter"/> for the index and data files
    /// from the specified <paramref name="indexFilePath"/> and
    /// <paramref name="dataFilePath"/>.
    /// </summary>
    /// <param name="indexFilePath">
    /// The path to the index file to create the <see cref="ISegmentWriter"/> for.
    /// </param>
    /// <param name="dataFilePath">
    /// The path to the data file to create the <see cref="ISegmentWriter"/> for.
    /// </param>
    /// <returns>
    /// An instance of <see cref="ISegmentWriter"/> that can be used to write
    /// the segment.
    /// </returns>
    protected virtual ISegmentWriter CreateSegmentWriter(
        string indexFilePath,
        string dataFilePath)
    {
        return new SegmentWriter(indexFilePath, dataFilePath);
    }

    /// <summary>
    /// Creates a <see cref="Segment{TKey, TValue}"/> for reading the segment
    /// with the given <paramref name="segmentId"/>.
    /// </summary>
    /// <param name="segmentId">
    /// The ID of the segment to read.
    /// </param>
    /// <returns>
    /// An <see cref="Segment{TKey, TValue}"/> representing the requested segment.
    /// </returns>
    private Segment<TKey, TValue> CreateReadOnlySegment(long segmentId)
    {
        (string indexFilePath, string dataFilePath) = GetFilePaths(segmentId);

        return CreateReadOnlySegment(segmentId, indexFilePath, dataFilePath);
    }

    /// <summary>
    /// Creates a <see cref="Segment{TKey, TValue}"/> for reading the segment
    /// with the given <paramref name="segmentId"/> and the specified file paths.
    /// </summary>
    /// <param name="segmentId">
    /// The ID of the segment to read.
    /// </param>
    /// <param name="indexFilePath">
    /// The path to the index file to create the <see cref="Segment{TKey, TValue}"/> for.
    /// </param>
    /// <param name="dataFilePath">
    /// The path to the data file to create the <see cref="Segment{TKey, TValue}"/> for.
    /// </param>
    /// <returns>
    /// An <see cref="Segment{TKey, TValue}"/> representing the requested segment.
    /// </returns>
    private Segment<TKey, TValue> CreateReadOnlySegment(
        long segmentId,
        string indexFilePath,
        string dataFilePath)
    {
        return new(
            segmentId,
            new(
                loggerFactory.CreateLogger<Driver<TKey, TValue>>(),
                CreateSegmentReader(indexFilePath, dataFilePath),
                entryFormatter
            ));
    }

    private (string indexFilePath, string dataFilePath) GetFilePaths(long segmentId)
    {
        string indexFileName = String.Format(SegmentsFileNamingConvention, segmentId) + IndexExtension;
        string dataFileName = String.Format(SegmentsFileNamingConvention, segmentId) + DataExtension;

        string indexFilePath = Path.Combine(segmentsDir.FullName, indexFileName);
        string dataFilePath = Path.Combine(segmentsDir.FullName, dataFileName);

        return (indexFilePath, dataFilePath);
    }
}
