using Moq;
using TeaSuite.KV.Policies;

namespace TeaSuite.KV.IO;

partial class DriverTests
{
    private readonly StoreSettings settings = new StoreSettings();
    private readonly Mock<IEnumerator<StoreEntry<int, int>>> mockEnumerator = new(MockBehavior.Strict);

    [Fact]
    public async Task WriteEntriesAsyncThrowsForReadOnlyDriver()
    {
        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await driver.WriteEntriesAsync(mockEnumerator.Object, settings, default));

        Assert.Equal("Cannot write in a non-writable segment.", ex.Message);
    }

    [Fact]
    public async Task WriteEntriesAsyncThrowsIfDisposed()
    {
        mockSegmentWriter.Setup(w => w.Dispose());
        mockSegmentWriter.Setup(w => w.DisposeAsync()).Returns(new ValueTask());

        InitWriteOnlyDriver();
        Assert.NotNull(driver);
        driver.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => driver.WriteEntriesAsync(mockEnumerator.Object, settings, default));
        mockSegmentWriter.Verify(w => w.Dispose(), Times.Once);

        // Prepare for async dispose.
        InitWriteOnlyDriver();

        Assert.NotNull(driver);
        await driver.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => driver.WriteEntriesAsync(mockEnumerator.Object, settings, default));
        mockSegmentWriter.Verify(w => w.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [InlineData(10, false)]
    [InlineData(10, true)]
    public async Task WriteEntriesAsyncReturnsEntryCount(int entryCount, bool indexEveryEntry)
    {
        using MemoryStream indexStream = new MemoryStream();
        using MemoryStream dataStream = new MemoryStream();

        IEnumerable<int> indexEnum = Enumerable.Range(0, entryCount);
        Func<int, int> keyFunc = (i) => i + 1000;

        if (indexEveryEntry)
        {
            settings.IndexPolicy = new DefaultIndexPolicy(100, 1);
        }

        List<StoreEntry<int, int>> entries = new List<StoreEntry<int, int>>(indexEnum
            .Select(i => i % 2 == 0 ? new StoreEntry<int, int>(keyFunc(i), i) : StoreEntry<int, int>.Delete(keyFunc(i))));
        mockSegmentWriter.Setup(w => w.OpenIndexForWriteAsync(default)).ReturnsAsync(indexStream);
        mockSegmentWriter.Setup(w => w.OpenDataForWriteAsync(default)).ReturnsAsync(dataStream);

        mockEntryFormatter
            .Setup(f => f.WriteKeyAsync(It.IsIn<int>(indexEnum.Select(keyFunc)), indexStream, default))
            .Returns(new ValueTask());
        mockEntryFormatter
            .Setup(f => f.WriteKeyAsync(It.IsIn<int>(indexEnum.Select(keyFunc)), dataStream, default))
            .Returns(new ValueTask());
        mockEntryFormatter
            .Setup(f => f.WriteValueAsync(
                It.IsIn<int>(Enumerable.Range(0, (entryCount + 1) / 2).Select(i => i * 2)), dataStream, default))
            .Returns(new ValueTask());

        InitWriteOnlyDriver();
        Assert.NotNull(driver);

        long numWritten = await driver.WriteEntriesAsync(entries.GetEnumerator(), settings, default);
        Assert.Equal((long)entryCount, numWritten);

        if (indexEveryEntry)
        {
            mockEntryFormatter.Verify(
                f => f.WriteKeyAsync(It.IsAny<int>(), indexStream, default),
                Times.Exactly(entryCount));
        }
        else
        {
            mockEntryFormatter.Verify(
                f => f.WriteKeyAsync(It.IsAny<int>(), indexStream, default),
                Times.Exactly(Math.Min(2, entryCount)));
        }

        mockEntryFormatter.Verify(f => f.WriteKeyAsync(It.IsAny<int>(), dataStream, default), Times.Exactly(entryCount));
        mockEntryFormatter.Verify(
            f => f.WriteValueAsync(It.IsAny<int>(), dataStream, default),
            Times.Exactly((entryCount + 1) / 2));

        byte[] actual = indexStream.ToArray();
        byte[] expected;
        if (BitConverter.IsLittleEndian)
        {
            expected = new byte[] { 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00 };
        }
        else
        {
            expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        }
        // Assert.True(indexBuffer[0..8].Equals((ReadOnlyMemory<byte>)expected));
        Assert.Equal(expected, actual.Take(8), EqualityComparer<byte>.Default);
    }
}
