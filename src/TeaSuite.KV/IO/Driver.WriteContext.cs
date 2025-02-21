using System;
using System.IO;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue> : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Defines the context for writing to a segment's data or index file.
    /// </summary>
    private readonly record struct WriteContext : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Initializes a new instance of <see cref="WriteContext"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to write to.
        /// </param>
        public WriteContext(Stream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Gets the <see cref="Stream"/> to write to.
        /// </summary>
        public Stream Stream { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Stream.Flush();
            Stream.Dispose();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await Stream.FlushAsync().ConfigureAwaitLib();
#if NETSTANDARD2_0
            Stream.Dispose();
#else
            await Stream.DisposeAsync().ConfigureAwaitLib();
#endif
        }
    }
}
