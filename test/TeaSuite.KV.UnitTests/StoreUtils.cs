using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV;

internal static class StoreUtils
{
    public static Mock<ISegmentManager<int, int>> CreateSegmentManager(params Segment<int, int>[] segments)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = new Mock<ISegmentManager<int, int>>(MockBehavior.Strict);

        mockSegmentManager.Setup(m => m.DiscoverSegments()).Returns(segments);

        return mockSegmentManager;
    }

    public static Segment<int, int> CreateSegment(long id, ISegmentReader reader, IEntryFormatter<int, int> formatter)
    {
        Driver<int, int> driver = new Driver<int, int>(NullLogger<Driver<int, int>>.Instance, reader, formatter);
        Segment<int, int> segment = new Segment<int, int>(id, driver);

        return segment;
    }

    public static ReaderContext CreateSegmentReader(int firstKey, int lastKey, params EntryFlags[] entryFlags)
    {
        Mock<ISegmentReader> reader = new Mock<ISegmentReader>(MockBehavior.Strict);
        Stream indexStream = StreamUtils.CreateIndexStream(false, Driver<int, int>.SegmentMetadata.CurrentVersion, 0, 0);
        Stream dataStream = StreamUtils.CreateDataStream(entryFlags);

        reader.Setup(r => r.OpenIndexForReadAsync(default)).ReturnsAsync(indexStream);
        reader.Setup(r => r.OpenDataForReadAsync(It.IsAny<long>(), It.IsAny<long?>(), default)).ReturnsAsync(dataStream);

        return new ReaderContext(reader, indexStream, dataStream);
    }

    public readonly struct ReaderContext
    {
        public ReaderContext(Mock<ISegmentReader> reader, Stream indexStream, Stream dataStream)
        {
            Reader = reader;
            IndexStream = indexStream;
            DataStream = dataStream;
        }

        public readonly Mock<ISegmentReader> Reader;

        public readonly Stream IndexStream;
        public readonly Stream DataStream;
    }
}
