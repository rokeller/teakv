using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using TeaSuite.KV.Policies;
using static TeaSuite.KV.StoreUtils;

namespace TeaSuite.KV;

partial class DefaultKeyValueStoreTests
{
    private readonly Mock<ILockingPolicy> mockLockingPolicy = new(MockBehavior.Strict);
    private readonly Mock<IDisposable> mockDisposable = new(MockBehavior.Loose);

    [Theory]
    [InlineAutoData]
    public void TryGetAcquiresReadLockBeforeCheckingInMemoryStore(int key)
    {
        DateTimeOffset? lockAcquired = null;
        DateTimeOffset? memStoreChecked = null;
        DateTimeOffset? lockReleased = null;
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockLockingPolicy
            .Setup(p => p.AcquireReadLock())
            .Callback(() => lockAcquired = DateTimeOffset.UtcNow)
            .Returns(mockDisposable.Object);

        mockMemStore
            .Setup(s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(() => memStoreChecked = DateTimeOffset.UtcNow)
            .Returns(false);

        mockDisposable.Setup(d => d.Dispose())
            .Callback(() => lockReleased = DateTimeOffset.UtcNow);

        Assert.False(store.TryGet(key, out int actualValue));
        Assert.True(lockAcquired.HasValue);
        Assert.True(memStoreChecked.HasValue);
        Assert.True(lockReleased.HasValue);
        Assert.True(lockAcquired < memStoreChecked);
        Assert.True(memStoreChecked < lockReleased);

        mockLockingPolicy.Verify(p => p.AcquireReadLock(), Times.Once);
        mockMemStore.Verify(
            s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
        mockDisposable.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineAutoData]
    public void SetAcquiresWriteLockBeforeWriting(int key, int value)
    {
        DateTimeOffset? lockAcquired = null;
        DateTimeOffset? memStoreWritten = null;
        DateTimeOffset? lockReleased = null;
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockLockingPolicy
            .Setup(p => p.AcquireWriteLock())
            .Callback(() => lockAcquired = DateTimeOffset.UtcNow)
            .Returns(mockDisposable.Object);

        mockMemStore
            .Setup(s => s.Set(It.Is<StoreEntry<int, int>>(
                e => e.Key == key && e.Value == value)))
            .Callback(() => memStoreWritten = DateTimeOffset.UtcNow);
        mockMemStore.Setup(s => s.Count).Returns(1);

        mockDisposable.Setup(d => d.Dispose())
            .Callback(() => lockReleased = DateTimeOffset.UtcNow);

        store.Set(key, value);

        Assert.True(lockAcquired.HasValue);
        Assert.True(memStoreWritten.HasValue);
        Assert.True(lockReleased.HasValue);
        Assert.True(lockAcquired < memStoreWritten);
        Assert.True(memStoreWritten < lockReleased);

        mockLockingPolicy.Verify(p => p.AcquireWriteLock(), Times.Once);
        mockMemStore.Verify(
            s => s.Set(It.IsAny<StoreEntry<int, int>>()), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
        mockDisposable.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineAutoData]
    public void DeleteAcquiresWriteLockBeforeWriting(int key)
    {
        DateTimeOffset? lockAcquired = null;
        DateTimeOffset? memStoreWritten = null;
        DateTimeOffset? lockReleased = null;
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockLockingPolicy
            .Setup(p => p.AcquireWriteLock())
            .Callback(() => lockAcquired = DateTimeOffset.UtcNow)
            .Returns(mockDisposable.Object);

        mockMemStore
            .Setup(s => s.Set(It.Is<StoreEntry<int, int>>(
                e => e.Key == key && e.IsDeleted)))
            .Callback(() => memStoreWritten = DateTimeOffset.UtcNow);
        mockMemStore.Setup(s => s.Count).Returns(1);

        mockDisposable.Setup(d => d.Dispose())
            .Callback(() => lockReleased = DateTimeOffset.UtcNow);

        store.Delete(key);

        Assert.True(lockAcquired.HasValue);
        Assert.True(memStoreWritten.HasValue);
        Assert.True(lockReleased.HasValue);
        Assert.True(lockAcquired < memStoreWritten);
        Assert.True(memStoreWritten < lockReleased);

        mockLockingPolicy.Verify(p => p.AcquireWriteLock(), Times.Once);
        mockMemStore.Verify(
            s => s.Set(It.IsAny<StoreEntry<int, int>>()), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
        mockDisposable.VerifyNoOtherCalls();
    }
}
