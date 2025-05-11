using static TeaSuite.KV.IO.StreamUtils;

namespace TeaSuite.KV.IO;

public sealed class StreamExtensionsTests
{
    [Fact]
    public void FillWithByteArrayReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(3, 12, 255);
        byte[] buffer = new byte[9];

        StreamExtensions.Fill(stream, buffer, buffer.Length);

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 3, buffer[i]);
        }

        Assert.Equal(255, stream.ReadByte());
    }

    [Fact]
    public void FillReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(3, 12, 255);

#if TestTargetsNetStandard
        byte[] buffer = new byte[9];
        StreamExtensions.Fill(stream, buffer, buffer.Length);
#else
        Span<byte> buffer = stackalloc byte[9];
        StreamExtensions.Fill(stream, buffer);
#endif

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 3, buffer[i]);
        }

        Assert.Equal(255, stream.ReadByte());
    }

    [Fact]
    public void FillWithByteArrayThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(25, 27, 255);

        EndOfStreamException ex = Assert.Throws<EndOfStreamException>(() =>
        {
            byte[] buffer = new byte[9];
            StreamExtensions.Fill(stream, buffer, buffer.Length);
        });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }

    [Fact]
    public void FillThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(25, 27, 255);

        EndOfStreamException ex = Assert.Throws<EndOfStreamException>(() =>
        {
#if TestTargetsNetStandard
            byte[] buffer = new byte[9];
            StreamExtensions.Fill(stream, buffer, buffer.Length);
#else
            Span<byte> buffer = stackalloc byte[9];
            StreamExtensions.Fill(stream, buffer);
#endif
        });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }

    [Fact]
    public async Task FillAsyncWithByteArrayReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(33, 42, 250);
        byte[] buffer = new byte[9];

        await StreamExtensions.FillAsync(stream, buffer, buffer.Length, default);

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 33, buffer[i]);
        }

        Assert.Equal(250, stream.ReadByte());
    }

    [Fact]
    public async Task FillAsyncReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(33, 42, 250);

#if TestTargetsNetStandard
        byte[] buf = new byte[9];
        Memory<byte> buffer = new(buf);
        await StreamExtensions.FillAsync(stream, buf, buf.Length, default);
#else
        Memory<byte> buffer = new(new byte[9]);
        await StreamExtensions.FillAsync(stream, buffer, default);
#endif

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 33, buffer.Span[i]);
        }

        Assert.Equal(250, stream.ReadByte());
    }

    [Fact]
    public async Task FillAsyncWithByteArrayThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(55, 57, 255);

        EndOfStreamException ex = await Assert.ThrowsAsync<EndOfStreamException>(
            async () =>
            {
                byte[] buffer = new byte[9];
                await StreamExtensions.FillAsync(stream, buffer, buffer.Length, default);
            });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }

    [Fact]
    public async Task FillAsyncThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(55, 57, 255);

        EndOfStreamException ex = await Assert.ThrowsAsync<EndOfStreamException>(
            async () =>
            {
#if TestTargetsNetStandard
                byte[] buffer = new byte[9];
                await StreamExtensions.FillAsync(stream, buffer, buffer.Length, default);
#else
                Memory<byte> buffer = new(new byte[9]);
                await StreamExtensions.FillAsync(stream, buffer, default);
#endif
            });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }
}
