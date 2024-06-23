using AutoFixture.Xunit2;

namespace TeaSuite.KV;

public sealed class NullWriteAheadLogTests
{
    private readonly NullWriteAheadLog<int, int> wal = new();

    [Theory, AutoData]
    public async Task NormalWalLifecycleDoesNothing(int key, int val)
    {
        Assert.False(wal.ShouldRecover());
        wal.Start();
        Assert.True(await wal.AnnounceWriteAsync(new StoreEntry<int, int>(key, val)));
        using (await wal.PrepareTransitionAsync()) { };
        using (await wal.CompleteTransitionAsync()) { };
        wal.Shutdown();
    }

    [Fact]
    public void RecoverThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => wal.Recover());
    }
}
