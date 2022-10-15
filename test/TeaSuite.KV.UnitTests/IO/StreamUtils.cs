using System.Security.Cryptography;
namespace TeaSuite.KV.IO;

internal static class StreamUtils
{
    public static void WriteRandom(int numBytes, Stream target)
    {
        Span<byte> buffer = stackalloc byte[numBytes];
        RandomNumberGenerator.Fill(buffer);

        target.Write(buffer);
    }

    public static Stream WrapNonSeekable(Stream stream)
    {
        return new StreamWrapper(stream) { CanSeekOverride = false, };
    }

    private sealed class StreamWrapper : Stream
    {
        private readonly Stream inner;
        public bool? CanSeekOverride { get; init;}

        public StreamWrapper(Stream inner)
        {
            this.inner = inner;
        }

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => CanSeekOverride.HasValue ? CanSeekOverride.Value : inner.CanSeek;

        public override bool CanWrite => inner.CanWrite;

        public override long Length => inner.Length;

        public override long Position { get => inner.Position; set => inner.Position = value; }

        public override void Flush()
        {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
        }
    }
}
