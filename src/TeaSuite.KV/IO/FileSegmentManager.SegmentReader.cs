using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

partial class FileSegmentManager<TKey, TValue>
{
    /// <summary>
    /// Implements <see cref="ISegmentReader"/> for file segments.
    /// </summary>
    private readonly struct SegmentReader : ISegmentReader
    {
        private readonly string indexFilePath;
        private readonly string dataFilePath;

        public SegmentReader(string indexFilePath, string dataFilePath)
        {
            this.indexFilePath = indexFilePath;
            this.dataFilePath = dataFilePath;
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenIndexForReadAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<Stream>(File.Open(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        /// <inheritdoc/>
        public ValueTask<Stream> OpenDataForReadAsync(
            long position,
            long? readWindow,
            CancellationToken cancellationToken)
        {
            Stream stream = File.Open(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(position, SeekOrigin.Begin);

            return new ValueTask<Stream>(stream);
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
