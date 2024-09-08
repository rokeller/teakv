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
        Mock<ISegmentWriter> mockWriter = new(MockBehavior.Loose);
        Mock<IEntryFormatter<string, int>> mockFormatter = new(MockBehavior.Loose);
        driver = new(
            NullLogger<Driver<string, int>>.Instance,
            mockWriter.Object,
            mockFormatter.Object);
        segment = new(123, driver);
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
        Segment<string, int> older = new(12, driver);
        Segment<string, int> newer = new(134, driver);
        Segment<string, int> same = new(123, driver);

        Assert.True(segment.CompareTo(older) < 0,
            "The segment must be sorted before the older segment.");
        Assert.True(segment.CompareTo(newer) > 0,
            "The segment must be sorted after the new segment.");

        Assert.Equal(0, segment.CompareTo(same));
    }
}
