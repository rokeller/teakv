using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

partial class PrimitiveFormatters
{
    /// <summary>
    /// Implements <see cref="IFormatter{T}"/> for <see cref="Guid"/> values.
    /// </summary>
    public readonly struct GuidFormatter : IFormatter<Guid>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GuidFormatter"/>.
        /// </summary>
        public GuidFormatter() { }

        /// <summary>
        /// The size of a Guid value in bytes.
        /// </summary>
        private const int GuidSize = 16;

        /// <inheritdoc/>
        public ValueTask<Guid> ReadAsync(Stream source, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = new byte[GuidSize];
            source.Fill(buffer, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[GuidSize];
            source.Fill(buffer);
#endif
            return new(new Guid(buffer));
        }

        /// <inheritdoc/>
        public ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
        {
            source.Skip(GuidSize);
            return default;
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(Guid value, Stream destination, CancellationToken cancellationToken)
        {
#if NETSTANDARD
            byte[] buffer = value.ToByteArray();
            destination.Write(buffer, 0, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[GuidSize];
            bool successful = value.TryWriteBytes(buffer);
            Debug.Assert(successful, "Writing the value to the byte buffer must have been successful.");
            destination.Write(buffer);
#endif
            return default;
        }
    }
}
