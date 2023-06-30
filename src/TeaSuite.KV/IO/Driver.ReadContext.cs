using System;
using System.IO;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Defines the context for reading from a segment's data or index file.
    /// </summary>
    private readonly record struct ReadContext : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ReadContext"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to read from.
        /// </param>
        public ReadContext(Stream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Gets the <see cref="Stream"/> to read from.
        /// </summary>
        public Stream Stream { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Stream.Dispose();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return Stream.DisposeAsync();
        }
    }
}
