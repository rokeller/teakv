namespace TeaSuite.KV;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNowWorks()
    {
        SystemClock clock = new();
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        Assert.InRange(
            clock.UtcNow,
            utcNow.AddMilliseconds(-100), utcNow.AddMilliseconds(100));
    }
}
