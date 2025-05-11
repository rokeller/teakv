using System.Collections;
using System.Reflection;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeaSuite.KV.IO;
using static TeaSuite.KV.StoreUtils;

namespace TeaSuite.KV;

partial class DefaultKeyValueStoreTests
{
    [Theory]
    [InlineAutoData]
    public void GetEntriesEnumeratorWrapsWithGardingEnumeratorForReadLock(int a, int b)
    {
        Range<int> range = new()
        {
            HasStart = true,
            Start = a,
            HasEnd = true,
            End = b,
        };
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockLockingPolicy.Setup(p => p.AcquireReadLock()).Returns(mockDisposable.Object);

        Mock<IEnumerator<StoreEntry<int, int>>> mockInMemEnum = new(MockBehavior.Strict);
        mockMemStore
            .Setup(s => s.GetOrderedEnumerator(range))
            .Returns(mockInMemEnum.Object);
        mockInMemEnum.Setup(e => e.Dispose());

        mockDisposable.Setup(d => d.Dispose());

        IEnumerator<StoreEntry<int, int>> enumerator = store.GetEntriesEnumerator(range);
        Assert.IsType<GuardingEnumerator<StoreEntry<int, int>>>(enumerator);

        mockLockingPolicy.Verify(p => p.AcquireReadLock(), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Never);
        mockInMemEnum.Verify(e => e.Dispose(), Times.Never);

        enumerator.Dispose();

        mockDisposable.Verify(d => d.Dispose(), Times.Once);
        mockInMemEnum.Verify(e => e.Dispose(), Times.Once);
    }

    [Theory]
    [InlineAutoData]
    public void GetEntriesEnumeratorWrapsWithGardingEnumeratorForNullReadLock(int a, int b)
    {
        Range<int> range = new()
        {
            HasStart = true,
            Start = a,
            HasEnd = true,
            End = b,
        };
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockLockingPolicy.Setup(p => p.AcquireReadLock()).Returns((IDisposable?)null);

        Mock<IEnumerator<StoreEntry<int, int>>> mockInMemEnum = new(MockBehavior.Strict);
        mockMemStore
            .Setup(s => s.GetOrderedEnumerator(range))
            .Returns(mockInMemEnum.Object);
        mockInMemEnum.Setup(e => e.Dispose());

        IEnumerator<StoreEntry<int, int>> enumerator = store.GetEntriesEnumerator(range);
        Assert.IsType<MergingEnumerator<StoreEntry<int, int>>>(enumerator);

        mockLockingPolicy.Verify(p => p.AcquireReadLock(), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Never);
        mockInMemEnum.Verify(e => e.Dispose(), Times.Never);

        enumerator.Dispose();

        mockDisposable.Verify(d => d.Dispose(), Times.Never);
        mockInMemEnum.Verify(e => e.Dispose(), Times.Once);
    }

    [Theory]
    [InlineAutoData]
    public void InMemoryEnumeratorForwardsToInner(int key, int value)
    {
        Type storeType = typeof(DefaultKeyValueStore<int, int>);
        Type enumeratorTypeGeneric = storeType.GetNestedType(
            "InMemoryEnumerator", BindingFlags.NonPublic)!;
        Type enumeratorType = enumeratorTypeGeneric.MakeGenericType(typeof(int), typeof(int));
        PropertyInfo readLockProp = enumeratorType.GetProperty("ReadLock")!;

        Mock<IEnumerator<StoreEntry<int, int>>> mockInner = new(MockBehavior.Strict);
        IEnumerator<StoreEntry<int, int>> enumerator =
            (IEnumerator<StoreEntry<int, int>>)Activator.CreateInstance(
                enumeratorType, mockDisposable.Object, mockInner.Object)!;

        StoreEntry<int, int> entry = new(key, value);

        mockInner.SetupSequence(e => e.MoveNext()).Returns(true).Returns(false);
        mockInner.SetupSequence(e => e.Current)
            .Returns(entry).Throws(new InvalidOperationException());
        mockInner.As<IEnumerator>().SetupSequence(e => e.Current)
            .Returns(entry).Throws(new InvalidOperationException());
        mockInner.Setup(e => e.Reset());
        mockInner.Setup(e => e.Dispose());

        Assert.True(enumerator.MoveNext());
        Assert.Equal(entry, enumerator.Current);
        Assert.Equal(entry, ((IEnumerator)enumerator).Current);
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();

        Assert.Same(mockDisposable.Object, readLockProp.GetValue(enumerator));

        enumerator.Dispose();

        mockInner.Verify(e => e.MoveNext(), Times.Exactly(2));
        mockInner.Verify(e => e.Current, Times.Once);
        mockInner.As<IEnumerator>().Verify(e => e.Current, Times.Once);
        mockInner.Verify(e => e.Reset(), Times.Once);
        mockInner.Verify(e => e.Dispose(), Times.Once);
    }

    [Fact]
    public void CreateEntriesEnumeratorSkipsWrappingIfNoInMemoryEnumeratorFirst()
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            NullWriteAheadLog<int, int>.Instance,
            mockMemFactory.Object,
            mockLockingPolicy.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        Mock<IEnumerator<StoreEntry<int, int>>> mockInner = new(MockBehavior.Strict);
#if NET8_0_OR_GREATER
        List<IEnumerator<StoreEntry<int, int>>> enumerators = [mockInner.Object,];
#else
        List<IEnumerator<StoreEntry<int, int>>> enumerators = new()
        {
            mockInner.Object,
        };
#endif

        Type storeType = store.GetType();
        MethodInfo? method = storeType.GetMethod(
            "CreateEntriesEnumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

#if NET8_0_OR_GREATER
        IEnumerator<StoreEntry<int, int>> result =
            (IEnumerator<StoreEntry<int, int>>)method.Invoke(store, [enumerators,])!;
#else
        IEnumerator<StoreEntry<int, int>> result =
            (IEnumerator<StoreEntry<int, int>>)method.Invoke(
                store, new object[] { enumerators, })!;
#endif
        Assert.IsType<MergingEnumerator<StoreEntry<int, int>>>(result);
    }
}
