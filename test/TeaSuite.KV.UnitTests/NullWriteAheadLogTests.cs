using AutoFixture.Xunit2;

namespace TeaSuite.KV;

public sealed class NullWriteAheadLogTests
{
    private readonly NullWriteAheadLog<int, int> wal = new();

    [Fact]
    public void StartNeverCallsRecoverAction()
    {
        int numInvocations = 0;
        wal.Start((_) => numInvocations++);
        Assert.Equal(0, numInvocations);
    }

    [Theory, AutoData]
    public async Task AnnounceWriteAsyncAlwaysReturnsTrue(int key, int val)
    {
        Assert.True(await wal.AnnounceWriteAsync(new StoreEntry<int, int>(key, val)));
    }

    [Fact]
    public async Task PrepareTransitionAsyncAlwaysReturnsDisposable()
    {
        using (await wal.PrepareTransitionAsync()) { };
    }

    [Fact]
    public async Task CompleteTransitionAsyncAlwaysReturnsDisposable()
    {
        using (await wal.CompleteTransitionAsync()) { };
    }

    [Fact]
    public void ShutdownDoesNothing()
    {
        wal.Shutdown();
    }
}
