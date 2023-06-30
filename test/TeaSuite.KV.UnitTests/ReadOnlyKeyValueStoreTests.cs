using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using TeaSuite.KV.IO.Formatters;
using static TeaSuite.KV.StoreUtils;

namespace TeaSuite.KV;

public sealed class ReadOnlyKeyValueStoreTests
{
    [Theory, AutoData]
    public void TryGetFindsNothingInZeroSegments(int key)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Assert.False(store.TryGet(key, out _));
    }

    [Theory]
    [InlineData(25, 125)]
    [InlineData(25, null)]
    [InlineData(75, 175)]
    [InlineData(75, null)]
    public void TryGetWorks(int key, int? expectedValue)
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49,
            EntryFlags.None, expectedValue.HasValue ? EntryFlags.None : EntryFlags.Deleted, EntryFlags.None);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99,
            EntryFlags.None, expectedValue.HasValue ? EntryFlags.None : EntryFlags.Deleted, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(25).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        // The first entry is skipped.
        mockEntryFormatter.SetupSequence(f => f.SkipReadValueAsync(reader1.DataStream, default))
            .Returns(new ValueTask()).ThrowsAsync(new EndOfStreamException());
        if (expectedValue.HasValue && key < 50)
        {
            mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
                .ReturnsAsync(expectedValue.Value).ThrowsAsync(new EndOfStreamException());
        }

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(75).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());
        // The first entry is skipped.
        mockEntryFormatter.SetupSequence(f => f.SkipReadValueAsync(reader2.DataStream, default))
            .Returns(new ValueTask()).ThrowsAsync(new EndOfStreamException());
        if (expectedValue.HasValue && key >= 50)
        {
            mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
                .ReturnsAsync(expectedValue.Value).ThrowsAsync(new EndOfStreamException());
        }

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Assert.Equal(expectedValue.HasValue, store.TryGet(key, out int value));
        if (expectedValue.HasValue)
        {
            Assert.Equal(expectedValue.Value, value);
        }
    }

    [Fact]
    public void SetThrowsNotSupportedException()
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => store.Set(1, 2));
        Assert.Equal("Setting values is not supported in a ReadOnlyKeyValueStore.", ex.Message);
    }

    [Fact]
    public void DeleteThrowsNotSupportedException()
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => store.Delete(1));
        Assert.Equal("Deleting values is not supported in a ReadOnlyKeyValueStore.", ex.Message);
    }

    [Fact]
    public void CloseWorks()
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        store.Close();
    }

    [Fact]
    public void GetEntriesEnumeratorWorks()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, EntryFlags.None, EntryFlags.None);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(1).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
            .ReturnsAsync(2).ReturnsAsync(3).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        using IEnumerator<StoreEntry<int, int>> enumerator = store.GetEntriesEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(0, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(0, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(49, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(1, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(50, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(2, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(99, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(3, enumerator.Current.Value);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void GetEntriesEnumeratorWithRangeWorks()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, EntryFlags.None, EntryFlags.None);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(1).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
            .ReturnsAsync(2).ReturnsAsync(3).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Range<int> range = new Range<int>()
        {
            HasStart = true,
            Start = 49,
            HasEnd = true,
            End = 99,
        };
        using IEnumerator<StoreEntry<int, int>> enumerator = store.GetEntriesEnumerator(range);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(49, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(1, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(50, enumerator.Current.Key);
        Assert.False(enumerator.Current.IsDeleted);
        Assert.Equal(2, enumerator.Current.Value);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void GetEnumeratorWorks()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, EntryFlags.None, EntryFlags.Deleted);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
            .ReturnsAsync(2).ReturnsAsync(3).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        using IEnumerator<KeyValuePair<int, int>> enumerator = store.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal(0, enumerator.Current.Key);
        Assert.Equal(0, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(50, enumerator.Current.Key);
        Assert.Equal(2, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(99, enumerator.Current.Key);
        Assert.Equal(3, enumerator.Current.Value);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void GetEnumeratorWithRangeWorks()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, EntryFlags.None, EntryFlags.Deleted);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
            .ReturnsAsync(2).ReturnsAsync(3).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Range<int> range = new Range<int>()
        {
            HasStart = true,
            Start = 49,
            HasEnd = true,
            End = 99,
        };
        using IEnumerator<KeyValuePair<int, int>> enumerator = store.GetEnumerator(range);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(50, enumerator.Current.Key);
        Assert.Equal(2, enumerator.Current.Value);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void GetEnumeratorWithRangeReturnsEmptyEnumeratorWhenNoOverlap()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, EntryFlags.None, EntryFlags.Deleted);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Range<int> range = new Range<int>()
        {
            HasStart = true,
            Start = 100,
        };
        AssertEmpty(store.GetEnumerator(range));

        range = new Range<int>()
        {
            HasEnd = true,
            End = 0,
        };
        AssertEmpty(store.GetEnumerator(range));
    }

    [Fact]
    public void GetEnumeratorWithRangeScansAsLittleAsNeeded()
    {
        ReaderContext reader1 = StoreUtils.CreateSegmentReader(0, 49, 3, EntryFlags.None, EntryFlags.None);
        ReaderContext reader2 = StoreUtils.CreateSegmentReader(50, 99, 3, EntryFlags.None, EntryFlags.None);
        Mock<IEntryFormatter<int, int>> mockEntryFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.IndexStream, default))
            .ReturnsAsync(0).ReturnsAsync(24).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.IndexStream, default))
            .ReturnsAsync(50).ReturnsAsync(74).ReturnsAsync(99).ThrowsAsync(new EndOfStreamException());

        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader1.DataStream, default))
            .ReturnsAsync(24).ReturnsAsync(49).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader1.DataStream, default))
            .ReturnsAsync(0).ReturnsAsync(1).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadKeyAsync(reader2.DataStream, default))
            .ReturnsAsync(50).ReturnsAsync(74).ThrowsAsync(new EndOfStreamException());
        mockEntryFormatter.SetupSequence(f => f.ReadValueAsync(reader2.DataStream, default))
            .ReturnsAsync(2).ReturnsAsync(3).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, reader1.Reader.Object, mockEntryFormatter.Object);
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, reader2.Reader.Object, mockEntryFormatter.Object);
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        ReadOnlyKeyValueStore<int, int> store = new ReadOnlyKeyValueStore<int, int>(
            NullLogger<ReadOnlyKeyValueStore<int, int>>.Instance, mockSegmentManager.Object);

        Range<int> range = new Range<int>()
        {
            HasStart = true,
            Start = 25,
            HasEnd = true,
            End = 51,
        };
        using IEnumerator<KeyValuePair<int, int>> enumerator = store.GetEnumerator(range);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(49, enumerator.Current.Key);
        Assert.Equal(1, enumerator.Current.Value);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(50, enumerator.Current.Key);
        Assert.Equal(2, enumerator.Current.Value);

        Assert.False(enumerator.MoveNext());
    }

    private static void AssertEmpty(IEnumerator<KeyValuePair<int, int>> enumerator)
    {
        using (enumerator)
        {
            Assert.False(enumerator.MoveNext());
        }
    }
}
