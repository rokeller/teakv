using Moq;
using static TeaSuite.KV.IO.StreamUtils;

namespace TeaSuite.KV.IO;

partial class DriverTests
{
    [Fact]
    public void GetEntryEnumeratorThrowsForWritableDriver()
    {
        InitWriteOnlyDriver();

        Assert.NotNull(driver);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => driver.GetEntryEnumerator());

        Assert.Equal("Cannot read in a non-readable segment.", ex.Message);
    }

    [Fact]
    public void GetEntryEnumeratorWithRangeThrowsForWritableDriver()
    {
        InitWriteOnlyDriver();

        Assert.NotNull(driver);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => driver.GetEntryEnumerator(new()
            {
                HasStart = true,
                Start = 1234,
            }));

        Assert.Equal("Cannot read in a non-readable segment.", ex.Message);
    }

    [Fact]
    public void OverlapsThrowsForWritableDriver()
    {
        InitWriteOnlyDriver();

        Assert.NotNull(driver);
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => driver.Overlaps(Range<int>.Unbounded));

        Assert.Equal("Cannot read in a non-readable segment.", ex.Message);
    }

    [Fact]
    public async Task GetEntryEnumeratorThrowsIfDisposed()
    {
        mockSegmentReader.Setup(r => r.Dispose());
        mockSegmentReader.Setup(r => r.DisposeAsync()).Returns(new ValueTask());

        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        driver.Dispose();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator());
        mockSegmentReader.Verify(r => r.Dispose(), Times.Once);

        // Prepare for async dispose.
        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        await driver.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator());
        mockSegmentReader.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEntryEnumeratorWithRangeThrowsIfDisposed()
    {
        mockSegmentReader.Setup(r => r.Dispose());
        mockSegmentReader.Setup(r => r.DisposeAsync()).Returns(new ValueTask());

        InitEmptyReadOnlyDriver();
        Range<int> range = new Range<int>()
        {
            HasEnd = true,
            End = 1234,
        };

        Assert.NotNull(driver);
        driver.Dispose();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator(range));
        mockSegmentReader.Verify(r => r.Dispose(), Times.Once);

        // Prepare for async dispose.
        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        await driver.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator(range));
        mockSegmentReader.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task OverlapsThrowsIfDisposed()
    {
        mockSegmentReader.Setup(r => r.Dispose());
        mockSegmentReader.Setup(r => r.DisposeAsync()).Returns(new ValueTask());

        InitEmptyReadOnlyDriver();
        Range<int> range = Range<int>.Unbounded;

        Assert.NotNull(driver);
        driver.Dispose();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator(range));
        mockSegmentReader.Verify(r => r.Dispose(), Times.Once);

        // Prepare for async dispose.
        InitEmptyReadOnlyDriver();

        Assert.NotNull(driver);
        await driver.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => driver.GetEntryEnumerator(range));
        mockSegmentReader.Verify(r => r.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void GetEntryEnumeratorReturnsEnumeratorForAllEntries(int numEntries)
    {
        // First entry is deleted, second not, third is deleted, fourth not, ...
        using Stream dataStream = CreateDataStream(
            Enumerable.Range(0, numEntries)
                .Select(i => i % 2 == 0 ? EntryFlags.Deleted : EntryFlags.None)
                .ToArray());
        mockSegmentReader.Setup(r => r.OpenDataForReadAsync(0, null, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitReadOnlyDriver(1000, 1000 + numEntries - 1);
        Assert.NotNull(driver);

        var readKeySequence = mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(dataStream, default));
        var readValueSequence = mockEntryFormatter
            .SetupSequence(f => f.ReadValueAsync(dataStream, default));
        for (int i = 0; i < numEntries; i++)
        {
            readKeySequence.Returns(new ValueTask<int>(1000 + i));
            if (i % 2 == 1)
            {
                readValueSequence.Returns(new ValueTask<int>(i));
            }
        }

        using IEnumerator<StoreEntry<int, int>> enumerator = driver.GetEntryEnumerator();

        for (int i = 0; i < numEntries; i++)
        {
            Assert.True(enumerator.MoveNext());

            StoreEntry<int, int> entry = enumerator.Current;
            Assert.Equal(1000 + i, entry.Key);
            if (i % 2 == 0)
            {
                Assert.True(entry.IsDeleted);
            }
            else
            {
                Assert.False(entry.IsDeleted);
                Assert.Equal(i, entry.Value);
            }
        }

        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void GetEntryEnumeratorReturnsEmptyEnumeratorWhenNoRangeOverlap()
    {
        // First entry is deleted, second not, third is deleted, fourth not, ...
        using Stream dataStream = CreateDataStream(EntryFlags.None, EntryFlags.None);
        mockSegmentReader.Setup(r => r.OpenDataForReadAsync(0, null, default))
            .Returns(new ValueTask<Stream>(dataStream));

        InitReadOnlyDriver(1000, 2000);
        Assert.NotNull(driver);

        Range<int> range = new()
        {
            HasStart = true,
            Start = 0,
            HasEnd = true,
            End = 100,
        };

        using IEnumerator<StoreEntry<int, int>> enumerator = driver
            .GetEntryEnumerator(range);

        Assert.False(enumerator.MoveNext());
    }
}
