using System.IO.MemoryMappedFiles;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV.IO;

public sealed class MemoryMappedFileSegmentManagerTests
{
    private readonly Fixture fixture = new Fixture();
    private readonly Mock<IEntryFormatter<int, int>> mockFormatter =
        new Mock<IEntryFormatter<int, int>>(MockBehavior.Loose);
    private readonly FileSegmentsOptions fileSegmentsOptions = new FileSegmentsOptions();
    private readonly Mock<IOptionsMonitor<FileSegmentsOptions>> mockFileSegmentsOptions =
        new Mock<IOptionsMonitor<FileSegmentsOptions>>(MockBehavior.Strict);
    private readonly MemoryMappedFileSegmentManager<int, int> manager;

    public MemoryMappedFileSegmentManagerTests()
    {
        mockFileSegmentsOptions.Setup(o => o.Get("KVStore<Int32,Int32>")).Returns(fileSegmentsOptions);
        fileSegmentsOptions.SegmentsDirectoryPath = Path.Combine(Path.GetTempPath(), "teakv", fixture.Create<string>());

        manager = new MemoryMappedFileSegmentManager<int, int>(
            NullLogger<MemoryMappedFileSegmentManager<int, int>>.Instance,
            NullLoggerFactory.Instance,
            mockFormatter.Object,
            mockFileSegmentsOptions.Object);
    }

    [Theory, AutoData]
    public async Task CreateNewSegmentWorks(long segmentId)
    {
        Segment<int, int> segment = manager.CreateNewSegment(segmentId);
        Assert.Equal(segmentId, segment.Id);
        Assert.NotNull(segment.Driver);

        List<StoreEntry<int, int>> entries = new List<StoreEntry<int, int>>()
        {
            new StoreEntry<int, int>(0, 100),
            new StoreEntry<int, int>(1, 101),
            new StoreEntry<int, int>(2, 102),
        };

        long entryCount = await segment.Driver.WriteEntriesAsync(entries.GetEnumerator(), new StoreSettings(), default);
        Assert.Equal(entries.Count, entryCount);

        (string indexFile, string dataFile) = GetFileNames(segmentId);
        Assert.True(File.Exists(indexFile));
        Assert.True(File.Exists(dataFile));

        segment.Driver.Dispose();
        await segment.Driver.DisposeAsync();
    }

    [Theory, AutoData]
    public async Task DeleteSegmentAsyncWorks(long segmentId)
    {
        (string indexFile, string dataFile) = GetFileNames(segmentId);
        File.Create(indexFile).Close();
        File.Create(dataFile).Close();

        Assert.True(File.Exists(indexFile));
        Assert.True(File.Exists(dataFile));

        await manager.DeleteSegmentAsync(segmentId, default);

        Assert.False(File.Exists(indexFile));
        Assert.False(File.Exists(dataFile));
    }

    [Theory, AutoData]
    public async Task MakeReadOnlyWorks(long segmentId)
    {
        Segment<int, int> seg = new Segment<int, int>(segmentId, null!);
        (string indexFile, string dataFile) = GetFileNames(segmentId);

        TestDataUtils.CopyTestData("segment_template.index", indexFile, respectEndianness: true);
        TestDataUtils.CopyTestData("segment_template.data", dataFile);

        mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(It.Is<FileStream>(stream => stream.Name.EndsWith(".index")), default))
            .ReturnsAsync(0).ReturnsAsync(2).ThrowsAsync(new EndOfStreamException());

        Segment<int, int> readonlySeg = manager.MakeReadOnly(seg);

        Assert.Equal(segmentId, readonlySeg.Id);

        readonlySeg.Driver.Dispose();
        await readonlySeg.Driver.DisposeAsync();
    }

    [Theory, AutoData]
    public void DiscoverSegmentsWorks(long[] segmentIds, long missingDataSegmentId)
    {
        var indexReadSeq = mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(It.Is<FileStream>(stream => stream.Name.EndsWith(".index")), default));
        string indexFile;
        string dataFile;

        for (int i = 0; i < segmentIds.Length; i++)
        {
            (indexFile, dataFile) = GetFileNames(segmentIds[i]);
            TestDataUtils.CopyTestData("segment_template.index", indexFile, respectEndianness: true);
            TestDataUtils.CopyTestData("segment_template.data", dataFile);

            // For each index file, 2 entries are read, followed by EoF.
            indexReadSeq = indexReadSeq.ReturnsAsync(0).ReturnsAsync(2).ThrowsAsync(new EndOfStreamException());
        }

        // Create one index file without a corresponding data file. This segment must not be "discovered".
        (indexFile, dataFile) = GetFileNames(missingDataSegmentId);
        TestDataUtils.CopyTestData("segment_template.index", indexFile, respectEndianness: true);

        SortedSet<Segment<int, int>> segments = new SortedSet<Segment<int, int>>(manager.DiscoverSegments());
        for (int i = 0; i < segmentIds.Length; i++)
        {
            Segment<int, int> test = new Segment<int, int>(segmentIds[i], null!);
            Assert.True(segments.Remove(test));
        }

        Assert.Empty(segments);
    }

    [Theory, AutoData]
    public async Task SegmentDataReadWorks(long segmentId, int entryValue)
    {
        const int EntryId = 234;
        Segment<int, int> seg = new Segment<int, int>(segmentId, null!);
        (string indexFile, string dataFile) = GetFileNames(segmentId);

        TestDataUtils.CopyTestData("segment_template.index", indexFile, respectEndianness: true);
        TestDataUtils.CopyTestData("segment_template.data", dataFile);

        mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(It.IsAny<FileStream>(), default))
            .ReturnsAsync(0).ReturnsAsync(EntryId).ThrowsAsync(new EndOfStreamException());
        mockFormatter
            .SetupSequence(f => f.ReadKeyAsync(It.Is<MemoryMappedViewStream>(stream =>
                stream.PointerOffset == 0x08 && /* the offset of the entry */
                stream.Position == 0x04 /* the last entry flags was read */), default))
            .ReturnsAsync(EntryId).ThrowsAsync(new EndOfStreamException());
        mockFormatter
            .SetupSequence(f => f.ReadValueAsync(It.Is<MemoryMappedViewStream>(stream =>
                stream.PointerOffset == 0x08 && /* the offset of the entry */
                stream.Position == 0x04 /* the last entry flags was read */), default))
            .ReturnsAsync(entryValue).ThrowsAsync(new EndOfStreamException());

        // Get an actual readable segment.
        seg = manager.MakeReadOnly(seg);
        StoreEntry<int, int>? entry = await seg.Driver.GetEntryAsync(EntryId, default);

        Assert.NotNull(entry);
        Assert.Equal(EntryId, entry.Value.Key);
        Assert.Equal(entryValue, entry.Value.Value);
    }

    private (string indexFile, string dataFile) GetFileNames(long segmentId)
    {
        string indexFile = Path.Combine(fileSegmentsOptions.SegmentsDirectoryPath,
                                        $"segment_{segmentId:d12}.index");
        string dataFile = Path.Combine(fileSegmentsOptions.SegmentsDirectoryPath,
                                       $"segment_{segmentId:d12}.data");

        return (indexFile, dataFile);
    }
}
