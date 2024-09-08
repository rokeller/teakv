using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using TeaSuite.KV.IO.Formatters;
using static TeaSuite.KV.IO.StreamUtils;

namespace TeaSuite.KV;

internal static class StoreUtils
{
    public delegate void TryGetCallback<TKey, TValue>(
        TKey key,
        out StoreEntry<TKey, TValue> entry
        )
        where TKey : IComparable<TKey>;

    public static string GetOptionsName<TKey, TValue>()
    {
        return OptionsExtensions.GetOptionsName<TKey, TValue>();
    }

    public static Mock<ISegmentManager<int, int>> CreateSegmentManager(
        params Segment<int, int>[] segments)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = new(MockBehavior.Strict);

        mockSegmentManager.Setup(m => m.DiscoverSegments()).Returns(segments);

        return mockSegmentManager;
    }

    public static Segment<int, int> CreateSegment(
        long id,
        ISegmentReader reader,
        IEntryFormatter<int, int> formatter
        )
    {
        Driver<int, int> driver = new(
            NullLogger<Driver<int, int>>.Instance, reader, formatter);
        Segment<int, int> segment = new(id, driver);

        return segment;
    }

    public static Segment<int, int> CreateSegment(
        long id,
        params StoreEntry<int, int>[] entries
        )
    {
        Mock<IEntryFormatter<int, int>> mockFormatter = new(MockBehavior.Strict);
        StoreEntry<int, int> first = entries.First();
        StoreEntry<int, int> last = entries.Last();

        ReaderContext context = CreateSegmentReader(
            first.Key,
            last.Key,
            entries
                .Select(e => e.IsDeleted ? EntryFlags.Deleted : EntryFlags.None)
                .ToArray());

        mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(context.IndexStream, default))
            .ReturnsAsync(first.Key)
            .ReturnsAsync(last.Key)
            .ThrowsAsync(new EndOfStreamException());
        var dataKeySeq = mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(context.DataStream, default));
        foreach (StoreEntry<int, int> entry in entries)
        {
            dataKeySeq.ReturnsAsync(entry.Key);
        }
        mockFormatter
            .Setup(f => f.SkipReadValueAsync(context.DataStream, default));
        var dataValueSeq = mockFormatter
            .SetupSequence(f => f.ReadValueAsync(context.DataStream, default));
        foreach (StoreEntry<int, int> entry in entries)
        {
            if (!entry.IsDeleted)
            {
                dataValueSeq.ReturnsAsync(entry.Value);
            }
        }

        IEnumerable<int> keys = entries.Select(e => e.Key);

        return CreateSegment(id, context.Reader.Object, mockFormatter.Object);
    }

    public static ReaderContext CreateSegmentReader(
        int firstKey,
        int lastKey,
        params EntryFlags[] entryFlags
        )
    {
        return CreateSegmentReader(firstKey, lastKey, 2, entryFlags);
    }

    public static ReaderContext CreateSegmentReader(
        int firstKey,
        int lastKey,
        int indexEntryCount,
        params EntryFlags[] entryFlags
        )
    {
        Mock<ISegmentReader> reader = new(MockBehavior.Strict);
        Stream indexStream = CreateIndexStream(
            false, Driver<int, int>.SegmentMetadata.CurrentVersion,
            Enumerable.Range(0, indexEntryCount)
                .Select((index) => (long)(index * sizeof(EntryFlags)))
                .ToArray());
        Stream dataStream = CreateDataStream(entryFlags);

        reader.Setup(r => r.OpenIndexForReadAsync(default)).ReturnsAsync(indexStream);
        reader.Setup(r => r.OpenDataForReadAsync(
            It.IsAny<long>(), It.IsAny<long?>(), default)).ReturnsAsync(dataStream);

        return new(reader, indexStream, dataStream);
    }

    public static Segment<int, int> CreateSegment(
        long id,
        ISegmentWriter writer,
        IEntryFormatter<int, int> formatter
        )
    {
        Driver<int, int> driver = new(
            NullLogger<Driver<int, int>>.Instance, writer, formatter);
        Segment<int, int> segment = new(id, driver);

        return segment;
    }

    public static WriterContext CreateSegmentWriter(
        MockBehavior mockBehavior = MockBehavior.Strict
        )
    {
        Mock<ISegmentWriter> writer = new(mockBehavior);
        Stream indexStream = new MemoryStream();
        Stream dataStream = new MemoryStream();

        writer.Setup(r => r.OpenIndexForWriteAsync(default)).ReturnsAsync(indexStream);
        writer.Setup(r => r.OpenDataForWriteAsync(default)).ReturnsAsync(dataStream);

        return new(writer, indexStream, dataStream);
    }

    public readonly struct ReaderContext
    {
        public ReaderContext(
            Mock<ISegmentReader> reader,
            Stream indexStream,
            Stream dataStream
            )
        {
            Reader = reader;
            IndexStream = indexStream;
            DataStream = dataStream;
        }

        public readonly Mock<ISegmentReader> Reader;

        public readonly Stream IndexStream;
        public readonly Stream DataStream;
    }

    public readonly struct WriterContext
    {
        public WriterContext(Mock<ISegmentWriter> writer, Stream indexStream, Stream dataStream)
        {
            Writer = writer;
            IndexStream = indexStream;
            DataStream = dataStream;
        }

        public readonly Mock<ISegmentWriter> Writer;

        public readonly Stream IndexStream;
        public readonly Stream DataStream;
    }
}
