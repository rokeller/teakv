namespace TeaSuite.KV.Policies;

public sealed class DefaultPersistPolicyTests
{
    private readonly DefaultPersistPolicy policy = new(12345, TimeSpan.FromMinutes(5));

    [Theory]
    [InlineData(1, "00:05:00")]
    [InlineData(10, "00:15:00")]
    [InlineData(12345, "00:01:00")]
    [InlineData(12456, "00:04:00")]
    public void ShouldPersistReturnsTrueWhenEitherThresholdIsCrossed(
        long entryCount,
        string rawTimeSinceLastPersist)
    {
        TimeSpan timeSinceLastPersist = TimeSpan.Parse(rawTimeSinceLastPersist);
        Assert.True(policy.ShouldPersist(entryCount, timeSinceLastPersist));
    }

    [Theory]
    [InlineData(1, "00:04:59.999")]
    [InlineData(12344, "00:00:20")]
    [InlineData(12344, "00:04:59.999")]
    public void ShouldPersistReturnsFalseWhenNeitherThresholdIsCrossed(
        long entryCount,
        string rawTimeSinceLastPersist)
    {
        TimeSpan timeSinceLastPersist = TimeSpan.Parse(rawTimeSinceLastPersist);
        Assert.False(policy.ShouldPersist(entryCount, timeSinceLastPersist));
    }


    [Fact]
    public void DefaultCtorEnforcesPersistAfter100KEntriesOr1Hour()
    {
        DefaultPersistPolicy policy = new();

        Assert.False(policy.ShouldPersist(
            99_999, TimeSpan.FromHours(1) - TimeSpan.FromMilliseconds(0.1)));
        Assert.True(policy.ShouldPersist(
            99_999, TimeSpan.FromHours(1)));
        Assert.True(policy.ShouldPersist(
            100_000, TimeSpan.FromHours(1) - TimeSpan.FromMilliseconds(0.1)));
        Assert.True(policy.ShouldPersist(
            100_000, TimeSpan.FromHours(1)));
    }
}
