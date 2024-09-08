using Moq;
using static TeaSuite.KV.IO.Driver<int, int>;
using static TeaSuite.KV.IO.StreamUtils;

namespace TeaSuite.KV.IO;

partial class DriverTests
{
    [Fact]
    public void ReaderCtorThrowsForEndiannessMismatch()
    {
        using Stream indexStream = CreateIndexStream(
            invertLittleEndian: true,
            SegmentMetadata.CurrentVersion);

        mockSegmentReader
            .Setup(r => r.OpenIndexForReadAsync(default))
            .Returns(new ValueTask<Stream>(indexStream));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => InitReadOnlyDriver());
        Assert.Equal(
            $"The machine is {(BitConverter.IsLittleEndian ? "little" : "big")} endian but the segment is not.",
            ex.Message);

        mockSegmentReader.Verify(r => r.OpenIndexForReadAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void ReaderCtorThrowsForUnsupportedVersion(uint version)
    {
        using Stream indexStream = CreateIndexStream(invertLittleEndian: false, version);

        mockSegmentReader
            .Setup(r => r.OpenIndexForReadAsync(default))
            .Returns(new ValueTask<Stream>(indexStream));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(
            () => InitReadOnlyDriver());
        Assert.Equal(
            $"Segments of version {version} are not supported.",
            ex.Message);

        mockSegmentReader.Verify(r => r.OpenIndexForReadAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public void ReaderCtorReadsIndex(int numIndexEntries)
    {
        // Run the test by initializing a read-only driver.
        InitReadOnlyDriver(Enumerable.Range(0, numIndexEntries)
            .Select(i => i + 1).ToArray());

        Assert.NotNull(driver);
        Assert.NotNull(driver.Metadata);
        Assert.Equal(SegmentMetadata.CurrentVersion, driver.Metadata.Value.Version);
        Assert.InRange(
            driver.Metadata.Value.Timestamp,
            DateTime.UtcNow.AddSeconds(-1),
            DateTime.UtcNow.AddSeconds(1));

        Assert.NotNull(driver.Index);

        static Action<IndexEntry> VerifyIndexEntry(int index)
        {
            return (IndexEntry entry) =>
            {
                Assert.Equal(index, entry.Id);
                Assert.Equal(index + 1, entry.Key);
                Assert.Equal(index * 4L, entry.Position);
            };
        }

        Assert.Collection(
            driver.Index,
            Enumerable.Range(0, numIndexEntries).Select(VerifyIndexEntry).ToArray());
        Assert.NotNull(driver.FirstIndexEntry);
        Assert.Equal(new(0, 1, 0), driver.FirstIndexEntry);
        Assert.NotNull(driver.LastIndexEntry);
        Assert.Equal(
            new(numIndexEntries - 1, numIndexEntries, (numIndexEntries - 1) * 4),
            driver.LastIndexEntry);

        mockSegmentReader.Verify(r => r.OpenIndexForReadAsync(default), Times.Once);
        mockEntryFormatter.Verify(
            f => f.ReadKeyAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Exactly(numIndexEntries + 1));
    }

    [Fact]
    public async Task GetEntryAsyncThrowsForWritableDriver()
    {
        InitWriteOnlyDriver();

        Assert.NotNull(driver);
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await driver.GetEntryAsync(123, default));

        Assert.Equal("Cannot read in a non-readable segment.", ex.Message);
    }

    [Fact]
    public async Task GetEntryAsyncThrowsIfDisposed()
    {
        mockSegmentReader.Setup(r => r.Dispose());
        mockSegmentReader.Setup(r => r.DisposeAsync()).Returns(new ValueTask());

        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        driver.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await driver.GetEntryAsync(123, default));
        mockSegmentReader.Verify(r => r.Dispose(), Times.Once);

        // Prepare for async dispose.
        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        await driver.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await driver.GetEntryAsync(123, default));
        mockSegmentReader.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(101)]
    [InlineData(1000)]
    public async Task GetEntryAsyncReturnsNullEntryForKeyOutOfRange(int key)
    {
        InitEmptyReadOnlyDriver(firstAndLastKey: 100);

        Assert.NotNull(driver);
        Assert.Null(await driver.GetEntryAsync(key, default));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public async Task GetEntryAsyncReturnsSingleEntry(int key, bool isDeleted)
    {
        using Stream dataStream = CreateDataStream(
            isDeleted ? EntryFlags.Deleted : EntryFlags.None);

        mockSegmentReader
            .Setup(r => r.OpenDataForReadAsync((long)key, null, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitEmptyReadOnlyDriver(firstAndLastKey: key);
        mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()))
            // The entry formatter will read one key from the data stream ...
            .Returns(new ValueTask<int>(key))
            // ... but no more keys afterwards.
            .ThrowsAsync(new EndOfStreamException());

        if (!isDeleted)
        {
            // It will also read a value for entries that aren't deleted.
            mockEntryFormatter
                .Setup(f => f.ReadValueAsync(
                    It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<int>(key * 2));
        }

        Assert.NotNull(driver);
        StoreEntry<int, int>? entry = await driver.GetEntryAsync(key, default);
        Assert.NotNull(entry);
        Assert.Equal(key, entry.Value.Key);
        if (isDeleted)
        {
            Assert.True(entry.Value.IsDeleted);
        }
        else
        {
            Assert.False(entry.Value.IsDeleted);
            Assert.Equal(key * 2, entry.Value.Value);
        }

        mockSegmentReader.Verify(
            r => r.OpenDataForReadAsync(
                It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(199)]
    public async Task GetEntryAsyncReturnsNullEntryForKeyNotFound(int key)
    {
        using Stream dataStream = CreateDataStream(EntryFlags.None, EntryFlags.None);
        mockSegmentReader.Setup(r => r.OpenDataForReadAsync(0, 4, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitReadOnlyDriver(Enumerable.Range(0, 2)
            .Select(i => (i + 1) * 100).ToArray());

        // The entry formatter will be used to read two more keys (the second already being beyond the scan range) ...
        mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(100))
            .Returns(new ValueTask<int>(200));

        // ... and skipping two values ...
        mockEntryFormatter
            .Setup(f => f.SkipReadValueAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        Assert.NotNull(driver);
        StoreEntry<int, int>? entry = await driver.GetEntryAsync(key, default);
        Assert.Null(entry);

        mockSegmentReader.Verify(
            r => r.OpenDataForReadAsync(
                It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockEntryFormatter.Verify(
            f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        mockEntryFormatter.Verify(
            f => f.SkipReadValueAsync(dataStream, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(199)]
    public async Task GetEntryAsyncReturnsNullEntryForReadingBeyondWindow(int key)
    {
        using Stream dataStream = CreateDataStream(EntryFlags.None);
        mockSegmentReader.Setup(r => r.OpenDataForReadAsync(0, 4, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitReadOnlyDriver(Enumerable.Range(0, 2).Select(i => (i + 1) * 100).ToArray());

        // The entry formatter will be used to read one more key ...
        mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(100));

        // ... and skipping one value ...
        mockEntryFormatter
            .Setup(f => f.SkipReadValueAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        Assert.NotNull(driver);
        StoreEntry<int, int>? entry = await driver.GetEntryAsync(key, default);
        Assert.Null(entry);

        mockSegmentReader.Verify(
            r => r.OpenDataForReadAsync(
                It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockEntryFormatter.Verify(
            f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()),
            Times.Once);
        mockEntryFormatter.Verify(
            f => f.SkipReadValueAsync(dataStream, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(101, false)]
    [InlineData(102, true)]
    [InlineData(148, false)]
    [InlineData(149, true)]
    [InlineData(151, false)]
    [InlineData(152, true)]
    [InlineData(198, false)]
    [InlineData(199, true)]
    [InlineData(150, false)]
    [InlineData(150, true)]
    public async Task GetEntryAsyncReturnsEntryForKeyFound(int key, bool isDeleted)
    {
        using Stream dataStream = CreateDataStream(
            EntryFlags.Deleted, isDeleted ? EntryFlags.Deleted : EntryFlags.None);
        // Whether we scan the first half (0) or the second half (1).
        int half = (key - 100) / 50;
        mockSegmentReader.Setup(r => r.OpenDataForReadAsync(half * 4, 4, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitReadOnlyDriver(Enumerable.Range(0, 3).Select(i => 100 + (i * 50)).ToArray());

        // The entry formatter will be used to read two more keys (the second already being the key searched) ...
        var dataStreamSequence = mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(half + 100))
            .Returns(new ValueTask<int>(key));

        // ... as well as reading one value.
        mockEntryFormatter
            .Setup(f => f.ReadValueAsync(dataStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(123456));

        Assert.NotNull(driver);
        StoreEntry<int, int>? entry = await driver.GetEntryAsync(key, default);
        Assert.NotNull(entry);
        Assert.Equal(key, entry.Value.Key);
        if (isDeleted)
        {
            Assert.True(entry.Value.IsDeleted);
        }
        else
        {
            Assert.False(entry.Value.IsDeleted);
            Assert.Equal(123456, entry.Value.Value);
        }

        mockSegmentReader.Verify(
            r => r.OpenDataForReadAsync(
                It.IsAny<long>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        mockEntryFormatter.Verify(
            f => f.ReadKeyAsync(dataStream, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        mockEntryFormatter.Verify(
            f => f.ReadValueAsync(dataStream, It.IsAny<CancellationToken>()),
            isDeleted ? Times.Never : Times.Once);
    }
}
