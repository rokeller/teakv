using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO.Formatters;
using static TeaSuite.KV.IO.Driver<int, int>;

namespace TeaSuite.KV.IO;

public sealed partial class DriverTests
{
    private readonly Mock<ISegmentReader> mockSegmentReader = new Mock<ISegmentReader>(MockBehavior.Strict);
    private readonly Mock<ISegmentWriter> mockSegmentWriter = new Mock<ISegmentWriter>(MockBehavior.Strict);
    private readonly Mock<IEntryFormatter<int, int>> mockEntryFormatter =
        new Mock<IEntryFormatter<int, int>>(MockBehavior.Strict);
    private Driver<int, int>? driver;

    private void InitReadOnlyDriver()
    {
        driver = new Driver<int, int>(
            NullLogger<Driver<int, int>>.Instance,
            mockSegmentReader.Object,
            mockEntryFormatter.Object);
    }

    private void InitReadOnlyDriver(params int[] keys)
    {
        using Stream indexStream = StreamUtils.CreateIndexStream(
            invertLittleEndian: false,
            SegmentMetadata.CurrentVersion,
            Enumerable.Range(0, keys.Length).Select(i => 4L * i).ToArray());
        mockSegmentReader.Setup(r => r.OpenIndexForReadAsync(default)).Returns(new ValueTask<Stream>(indexStream));
        var indexStreamSequence = mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(indexStream, It.IsAny<CancellationToken>()));

        for (int i = 0; i < keys.Length; i++)
        {
            indexStreamSequence = indexStreamSequence.Returns(new ValueTask<int>(keys[i]));
        }
        indexStreamSequence.ThrowsAsync(new EndOfStreamException());

        // Run the test by initializing a read-only driver.
        InitReadOnlyDriver();
    }

    private void InitEmptyReadOnlyDriver(int firstAndLastKey = 0)
    {
        using Stream indexStream = StreamUtils.CreateIndexStream(
            invertLittleEndian: false,
            SegmentMetadata.CurrentVersion,
            Enumerable.Range(firstAndLastKey, 1).Select(i => (long)i).ToArray());
        mockSegmentReader.Setup(r => r.OpenIndexForReadAsync(default)).Returns(new ValueTask<Stream>(indexStream));
        mockEntryFormatter
            .SetupSequence(f => f.ReadKeyAsync(indexStream, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(firstAndLastKey))
            .ThrowsAsync(new EndOfStreamException())
            ;

        // Run the test by initializing a read-only driver.
        InitReadOnlyDriver();
    }

    private void InitWriteOnlyDriver()
    {
        driver = new Driver<int, int>(
            NullLogger<Driver<int, int>>.Instance,
            mockSegmentWriter.Object,
            mockEntryFormatter.Object);
    }
}
