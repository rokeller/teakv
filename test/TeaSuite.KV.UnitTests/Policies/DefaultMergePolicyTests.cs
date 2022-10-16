namespace TeaSuite.KV.Policies;

public sealed class DefaultMergePolicyTests
{
    private readonly DefaultMergePolicy policy = new DefaultMergePolicy(4);

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(100)]
    public void ShouldMergeReturnsTrueWhenThresholdCrossed(long numSegments)
    {
        Assert.True(policy.ShouldMerge(numSegments));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ShouldMergeReturnsFalseWhenThresholdNotCrossed(long numSegments)
    {
        Assert.False(policy.ShouldMerge(numSegments));
    }

    [Fact]
    public void DefaultCtorEnforcesMergeStartingAt2Segments()
    {
        DefaultMergePolicy policy = new DefaultMergePolicy();

        Assert.False(policy.ShouldMerge(0));
        Assert.False(policy.ShouldMerge(1));
        Assert.True(policy.ShouldMerge(2));
        Assert.True(policy.ShouldMerge(3));
        Assert.True(policy.ShouldMerge(5));
    }
}
