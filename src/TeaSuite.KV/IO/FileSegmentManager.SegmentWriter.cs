using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

partial class FileSegmentManager<TKey, TValue>
{
    /// <summary>
    /// Implements <see cref="ISegmentWriter"/> for file segments.
    /// </summary>
    private readonly struct SegmentWriter : ISegmentWriter
    {
        private readonly string indexFilePath;
        private readonly string dataFilePath;

        public SegmentWriter(string indexFilePath, string dataFilePath)
        {
            this.indexFilePath = indexFilePath;
            this.dataFilePath = dataFilePath;
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenIndexForWriteAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<Stream>(File.Open(indexFilePath, FileMode.Create, FileAccess.Write, FileShare.None));
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenDataForWriteAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<Stream>(File.Open(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None));
        }

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
