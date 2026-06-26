using System.Security.Cryptography;
using Moq;

namespace TeaSuite.KV.IO.Formatters;

public sealed class DefaultEntryFormatterTests
{
    private readonly Mock<IFormatter<string>> mockStringFormatter = new(MockBehavior.Strict);
    private readonly Mock<IFormatter<int>> mockInt32Formatter = new(MockBehavior.Strict);
    private readonly DefaultEntryFormatter<string, int> formatter;

    public DefaultEntryFormatterTests()
    {
        formatter = new(mockStringFormatter.Object, mockInt32Formatter.Object);
    }

    [Fact]
    public async Task ReadKeyAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        mockStringFormatter
            .Setup(f => f.ReadAsync(memstr, It.IsAny<CancellationToken>()))
            .ReturnsAsync("abc");

        string key = await formatter.ReadKeyAsync(memstr, TestContext.Current.CancellationToken);
        Assert.Equal("abc", key);

        mockStringFormatter.Verify(
            f => f.ReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReadValueAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        int expected = RandomNumberGenerator.GetInt32(Int32.MaxValue);
        mockInt32Formatter
            .Setup(f => f.ReadAsync(memstr, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        int value = await formatter.ReadValueAsync(memstr, TestContext.Current.CancellationToken);
        Assert.Equal(expected, value);

        mockInt32Formatter.Verify(
            f => f.ReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SkipReadValueAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        int expected = RandomNumberGenerator.GetInt32(Int32.MaxValue);
        mockInt32Formatter
            .Setup(f => f.SkipReadAsync(memstr, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        await formatter.SkipReadValueAsync(memstr, TestContext.Current.CancellationToken);

        mockInt32Formatter.Verify(
            f => f.SkipReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteKeyAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        mockStringFormatter
            .Setup(f => f.WriteAsync("asdf", memstr, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        await formatter.WriteKeyAsync("asdf", memstr, TestContext.Current.CancellationToken);

        mockStringFormatter.Verify(
            f => f.WriteAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteValueAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        mockInt32Formatter
            .Setup(f => f.WriteAsync(345, memstr, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        await formatter.WriteValueAsync(345, memstr, TestContext.Current.CancellationToken);

        mockInt32Formatter.Verify(
            f => f.WriteAsync(
                It.IsAny<int>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
