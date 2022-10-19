using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV.IO;

public sealed class SegmentTests
{
    private readonly Driver<string, int> driver;
    private readonly Segment<string, int> segment;

    public SegmentTests()
    {
        Mock<ISegmentWriter> mockWriter = new Mock<ISegmentWriter>(MockBehavior.Loose);
        Mock<IEntryFormatter<string, int>> mockFormatter = new Mock<IEntryFormatter<string, int>>(MockBehavior.Loose);
        driver = new Driver<string, int>(
            NullLogger<Driver<string, int>>.Instance,
            mockWriter.Object,
            mockFormatter.Object);
        segment = new Segment<string, int>(123, driver);
    }

    [Fact]
    public void PropertiesWork()
    {
        Assert.Equal(123L, segment.Id);
        Assert.Same(driver, segment.Driver);
    }

    [Fact]
    public void CompareToWorks()
    {
        Segment<string, int> older = new Segment<string, int>(12, driver);
        Segment<string, int> newer = new Segment<string, int>(134, driver);
        Segment<string, int> same = new Segment<string, int>(123, driver);

        Assert.True(segment.CompareTo(older) < 0, "The segment must be sorted before the older segment.");
        Assert.True(segment.CompareTo(newer) > 0, "The segment must be sorted after the new segment.");

        Assert.Equal(0, segment.CompareTo(same));
    }
}
