using static TeaSuite.KV.IO.StreamUtils;

namespace TeaSuite.KV.IO;

public sealed class StreamExtensionsTests
{
    [Fact]
    public void FillReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(3, 12, 255);
        Span<byte> buffer = stackalloc byte[9];

        StreamExtensions.Fill(stream, buffer);

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 3, buffer[i]);
        }

        Assert.Equal(255, stream.ReadByte());
    }

    [Fact]
    public void FillThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(25, 27, 255);

        EndOfStreamException ex = Assert.Throws<EndOfStreamException>(() =>
        {
            Span<byte> buffer = stackalloc byte[9];
            StreamExtensions.Fill(stream, buffer);
        });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }


    [Fact]
    public async Task FillAsyncReadsUntilFull()
    {
        using Stream stream = CreateSequenceStream(33, 42, 250);
        Memory<byte> buffer = new(new byte[9]);

        await StreamExtensions.FillAsync(stream, buffer, default);

        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i + 33, buffer.Span[i]);
        }

        Assert.Equal(250, stream.ReadByte());
    }

    [Fact]
    public async Task FillAsyncThrowsWhenNotEnoughData()
    {
        using Stream stream = CreateSequenceStream(55, 57, 255);

        EndOfStreamException ex = await Assert.ThrowsAsync<EndOfStreamException>(
            async () =>
            {
                Memory<byte> buffer = new(new byte[9]);
                await StreamExtensions.FillAsync(stream, buffer, default);
            });

        Assert.Equal("Expected at least 6 more bytes.", ex.Message);
    }
}
