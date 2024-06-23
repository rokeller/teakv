using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV;

public sealed partial class FileWriteAheadLogTests
{
    private readonly string optionsName = StoreUtils.GetOptionsName<int, string>();
    private readonly FileWriteAheadLogSettings walSettings = new FileWriteAheadLogSettings();
    private readonly Mock<IOptionsMonitor<FileWriteAheadLogSettings>> mockSettings =
        new Mock<IOptionsMonitor<FileWriteAheadLogSettings>>(MockBehavior.Strict);
    private readonly Mock<ISystemClock> mockClock = new Mock<ISystemClock>(MockBehavior.Strict);
    private readonly DateTimeOffset utcNow = new DateTimeOffset(2024, 6, 23, 12, 14, 15, 0, TimeSpan.Zero);
    private readonly FileWriteAheadLog<int, string> wal;

    public FileWriteAheadLogTests()
    {
        mockSettings.Setup(o => o.Get(optionsName)).Returns(walSettings);
        mockClock.SetupGet(c => c.UtcNow).Returns(utcNow);

        walSettings.ReservedSize = 128;
        walSettings.LogDirectoryPath = Path.Combine(
            Path.GetTempPath(), "FileWriteAheadLogTests", Guid.NewGuid().ToString("N"));

        wal = new FileWriteAheadLog<int, string>(
            NullLogger<FileWriteAheadLog<int, string>>.Instance,
            new PrimitiveFormatters.Int32Formatter(),
            new PrimitiveFormatters.StringFormatter(),
            mockSettings.Object,
            mockClock.Object);
    }

    [Fact]
    public void CtorCreatesWalDir()
    {
        walSettings.LogDirectoryPath = Path.Combine(
            Path.GetTempPath(), "FileWriteAheadLogTests", Guid.NewGuid().ToString("N"));

        Assert.False(Directory.Exists(walSettings.LogDirectoryPath));
        new FileWriteAheadLog<int, string>(
            NullLogger<FileWriteAheadLog<int, string>>.Instance,
            new PrimitiveFormatters.Int32Formatter(),
            new PrimitiveFormatters.StringFormatter(),
            mockSettings.Object,
            mockClock.Object);
        Assert.True(Directory.Exists(walSettings.LogDirectoryPath));
    }

    [Fact]
    public void StartCreatesNewWal()
    {
        using (wal)
        {
            wal.Start();
        }

        mockSettings.Verify(o => o.Get(optionsName), Times.Once());
        mockSettings.VerifyAll();
        mockClock.Verify(o => o.UtcNow, Times.Once());
        mockClock.VerifyAll();

        byte[] expectedWal = ConstructNonClosedWal();
        byte[] actualWal = File.ReadAllBytes(
            Path.Combine(walSettings.LogDirectoryPath, ".wal.open"));

        Assert.Equal(expectedWal, actualWal);
    }

    [Theory, AutoData]
    public async Task AnnounceWriteAsyncAppendsWriteEntry(int key, string val)
    {
        using (wal)
        {
            wal.Start();
            bool res = await wal.AnnounceWriteAsync(new StoreEntry<int, string>(key, val));
            Assert.True(res);
        }

        mockSettings.Verify(o => o.Get(optionsName), Times.Once());
        mockSettings.VerifyAll();
        mockClock.Verify(o => o.UtcNow, Times.Once());
        mockClock.VerifyAll();

        byte[] expectedWal = ConstructWalWithWriteEntry(key, val);
        byte[] actualWal = File.ReadAllBytes(
            Path.Combine(walSettings.LogDirectoryPath, ".wal.open"));

        Assert.Equal(expectedWal, actualWal);
    }

    [Theory, AutoData]
    public async Task AnnounceWriteAsyncAppendsDeleteEntry(int key)
    {
        using (wal)
        {
            wal.Start();
            bool res = await wal.AnnounceWriteAsync(StoreEntry<int, string>.Delete(key));
            Assert.True(res);
        }

        mockSettings.Verify(o => o.Get(optionsName), Times.Once());
        mockSettings.VerifyAll();
        mockClock.Verify(o => o.UtcNow, Times.Once());
        mockClock.VerifyAll();

        byte[] expectedWal = ConstructWalWithDeleteEntry(key);
        byte[] actualWal = File.ReadAllBytes(
            Path.Combine(walSettings.LogDirectoryPath, ".wal.open"));

        Assert.Equal(expectedWal, actualWal);
    }

    [Theory, AutoData]
    public async Task AnnounceWriteAsyncThrowsWhenWalNotStarted(int key)
    {
        using (wal)
        {
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await wal.AnnounceWriteAsync(StoreEntry<int, string>.Delete(key)));
            Assert.Equal("Start must be called before the first write operation.", ex.Message);
        }
    }

    [Fact]
    public async Task PrepareTransitionAsyncPreparesWalTransition()
    {
        using (wal)
        {
            wal.Start();
            using (await wal.PrepareTransitionAsync())
            { }
        }

        mockSettings.Verify(o => o.Get(optionsName), Times.Once());
        mockSettings.VerifyAll();
        mockClock.Verify(o => o.UtcNow, Times.Exactly(3 /* 2 for new WALs, 1 for closed WAL */));
        mockClock.VerifyAll();

        // Check that the closed WAL is correct.
        byte[] expectedWal = ConstructClosedWal();
        byte[] actualWal = File.ReadAllBytes(
            Path.Combine(walSettings.LogDirectoryPath, ".wal.closed"));

        Assert.Equal(expectedWal, actualWal);

        // Check that the new open WAL is correct.
        expectedWal = ConstructNonClosedWal();
        actualWal = File.ReadAllBytes(
            Path.Combine(walSettings.LogDirectoryPath, ".wal.open"));

        Assert.Equal(expectedWal, actualWal);
    }

    [Fact]
    public async Task CompleteTransitionAsyncDeletesClosedWal()
    {
        string closedFilePath = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (File.Create(closedFilePath))
        {
        }

        Assert.True(File.Exists(closedFilePath));

        using (wal)
        {
            using (await wal.CompleteTransitionAsync())
            { }
        }

        Assert.False(File.Exists(closedFilePath));
    }

    [Fact]
    public void ShutdownDeletesWal()
    {
        string openFilePath = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (wal)
        {
            wal.Start();
            Assert.True(File.Exists(openFilePath));
            wal.Shutdown();
            Assert.False(File.Exists(openFilePath));
        }

        mockSettings.Verify(o => o.Get(optionsName), Times.Once());
        mockSettings.VerifyAll();
        mockClock.Verify(o => o.UtcNow, Times.Exactly(2));
        mockClock.VerifyAll();
    }
}
