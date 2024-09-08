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
            .Setup(f => f.ReadAsync(memstr, default))
            .ReturnsAsync("abc");

        string key = await formatter.ReadKeyAsync(memstr, default);
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
            .Setup(f => f.ReadAsync(memstr, default))
            .ReturnsAsync(expected);

        int value = await formatter.ReadValueAsync(memstr, default);
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
            .Setup(f => f.SkipReadAsync(memstr, default))
            .Returns(new ValueTask());

        await formatter.SkipReadValueAsync(memstr, default);

        mockInt32Formatter.Verify(
            f => f.SkipReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task WriteKeyAsyncUsesKeyFormatter()
    {
        using MemoryStream memstr = new();

        mockStringFormatter
            .Setup(f => f.WriteAsync("asdf", memstr, default))
            .Returns(new ValueTask());

        await formatter.WriteKeyAsync("asdf", memstr, default);

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
            .Setup(f => f.WriteAsync(345, memstr, default))
            .Returns(new ValueTask());

        await formatter.WriteValueAsync(345, memstr, default);

        mockInt32Formatter.Verify(
            f => f.WriteAsync(
                It.IsAny<int>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
