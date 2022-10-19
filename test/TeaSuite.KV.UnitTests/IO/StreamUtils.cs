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

    public static Stream CreateSequenceStream(int startInclusive, int endExclusive, int sentinel)
    {
        byte[] buffer = new byte[endExclusive - startInclusive + 1];

        for (int i = startInclusive; i < endExclusive; i++)
        {
            buffer[i - startInclusive] = (byte)i;
        }

        buffer[^1] = (byte)sentinel;

        return new MemoryStream(buffer);
    }

    public static Stream CreateIndexStream(bool invertLittleEndian, uint version, params long[] offsetPositions)
    {
        byte[] buffer = new byte[4 + 4 + 8 /* metadata */ + offsetPositions.Length * 8];
        Span<byte> writable = buffer;
        if (invertLittleEndian)
        {
            BitConverter.TryWriteBytes(writable, !BitConverter.IsLittleEndian ? 0x01_00_00_01 : 0x00_00_00_00);
        }
        else
        {
            BitConverter.TryWriteBytes(writable, BitConverter.IsLittleEndian ? 0x01_00_00_01 : 0x00_00_00_00);
        }
        writable = writable.Slice(4);
        BitConverter.TryWriteBytes(writable, version);
        writable = writable.Slice(4);
        BitConverter.TryWriteBytes(writable, DateTime.UtcNow.Ticks);
        writable = writable.Slice(8);

        foreach (long offsetPosition in offsetPositions)
        {
            BitConverter.TryWriteBytes(writable, offsetPosition);
            writable = writable.Slice(8);
        }

        return new MemoryStream(buffer);
    }

    public static Stream CreateDataStream(params EntryFlags[] entryFlags)
    {
        byte[] buffer = new byte[sizeof(EntryFlags) * entryFlags.Length];
        Span<byte> writable = buffer;

        for (int i = 0; i < entryFlags.Length; i++)
        {
            BitConverter.TryWriteBytes(writable, (uint)entryFlags[i]);
            writable = writable.Slice(sizeof(EntryFlags));
        }

        return new MemoryStream(buffer);
    }

    public static Stream WrapNonSeekable(Stream stream)
    {
        return new StreamWrapper(stream) { CanSeekOverride = false, };
    }

    private sealed class StreamWrapper : Stream
    {
        private readonly Stream inner;
        public bool? CanSeekOverride { get; init; }

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
