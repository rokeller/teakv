using System.Security.Cryptography;
namespace TeaSuite.KV.Policies;

public sealed class DefaultIndexPolicyTests
{
    private readonly DefaultIndexPolicy policy = new(123, 456);

    [Theory]
    [InlineData(1, 456)]
    [InlineData(10, 457)]
    [InlineData(123, 1)]
    [InlineData(124, 2)]
    public void ShouldIndexReturnsTrueWhenEitherThresholdIsCrossed(
        long bytesOffset,
        long entriesOffset)
    {
        Assert.True(policy.ShouldIndex(
            bytesOffset, entriesOffset, RandomNumberGenerator.GetInt32(Int32.MaxValue)));
    }

    [Theory]
    [InlineData(1, 455)]
    [InlineData(122, 455)]
    [InlineData(122, 1)]
    public void ShouldIndexReturnsFalseWhenNeitherThresholdIsCrossed(
        long bytesOffset,
        long entriesOffset)
    {
        Assert.False(policy.ShouldIndex(
            bytesOffset, entriesOffset, RandomNumberGenerator.GetInt32(Int32.MaxValue)));
    }

    [Fact]
    public void DefaultCtorEnforces2048BytesAnd100EntriesOffsets()
    {
        DefaultIndexPolicy policy = new();

        Assert.False(policy.ShouldIndex(
            2047, 99, RandomNumberGenerator.GetInt32(Int32.MaxValue)));
        Assert.True(policy.ShouldIndex(
            2048, 99, RandomNumberGenerator.GetInt32(Int32.MaxValue)));
        Assert.True(policy.ShouldIndex(
            2047, 100, RandomNumberGenerator.GetInt32(Int32.MaxValue)));
    }
}
