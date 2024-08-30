using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using TeaSuite.KV.Policies;

namespace TeaSuite.KV;

partial class DefaultKeyValueStoreTests
{
    private readonly Mock<IWriteAheadLog<int, int>> mockWal =
        new(MockBehavior.Strict);
    private readonly Mock<IEnumerator<StoreEntry<int, int>>> mockWalEnumerator =
        new(MockBehavior.Strict);

    [Theory, AutoData]
    public void CtorStartsWalWithRecovery(int key1, int val1, int key2, int val2)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        List<StoreEntry<int, int>> recoveredEntries = new()
        {
            new(key1, val1),
            new(key2, val2),
        };

        mockWalEnumerator.SetupSequence(e => e.MoveNext())
            .Returns(true).Returns(true).Returns(false);
        mockWalEnumerator.SetupSequence(e => e.Current)
            .Returns(recoveredEntries[0]).Returns(recoveredEntries[1]);
        mockWalEnumerator.Setup(e => e.Dispose());

        mockWal
            .Setup(w => w.Start(It.IsAny<Action<IEnumerator<StoreEntry<int, int>>>?>()))
            .Callback((Action<IEnumerator<StoreEntry<int, int>>>? recoverAction) =>
            {
                Assert.NotNull(recoverAction);
                recoverAction(mockWalEnumerator.Object);
            });
        mockWal
            .Setup(w => w.AnnounceWriteAsync(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[0]))))
            .Returns(new ValueTask<bool>(true));
        mockWal
            .Setup(w => w.AnnounceWriteAsync(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[1]))))
            .Returns(new ValueTask<bool>(true));

        mockMemStore.Setup(s => s.Set(It.IsAny<StoreEntry<int, int>>()));
        mockMemStore.SetupGet(s => s.Count).Returns(1); // Doesn't really matter.

        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockWal.Object,
            mockMemFactory.Object,
            NullLockingPolicy.Instance,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockWal.Verify(
            w => w.AnnounceWriteAsync(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[0]))),
            Times.Once);
        mockWal.Verify(
            w => w.AnnounceWriteAsync(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[1]))),
            Times.Once);
        mockMemStore.Verify(
            s => s.Set(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[0]))),
            Times.Once);
        mockMemStore.Verify(
            s => s.Set(It.Is<StoreEntry<int, int>>(
                (StoreEntry<int, int> e) => e.Equals(recoveredEntries[1]))),
            Times.Once);
    }
}
