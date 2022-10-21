using AutoFixture.Xunit2;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TeaSuite.KV.Data;
using TeaSuite.KV.IO;
using TeaSuite.KV.IO.Formatters;
using TeaSuite.KV.Policies;
using static TeaSuite.KV.StoreUtils;

namespace TeaSuite.KV;

public sealed class DefaultKeyValueStoreTests
{
    private readonly Fixture fixture = new Fixture();

    private readonly Mock<IMemoryKeyValueStoreFactory<int, int>> mockMemFactory =
        new Mock<IMemoryKeyValueStoreFactory<int, int>>(MockBehavior.Strict);
    private readonly Mock<IMemoryKeyValueStore<int, int>> mockMemStore =
        new Mock<IMemoryKeyValueStore<int, int>>(MockBehavior.Strict);
    private readonly StoreOptions<int, int> options = new StoreOptions<int, int>();
    private readonly Mock<IOptionsSnapshot<StoreOptions<int, int>>> mockOptions =
        new Mock<IOptionsSnapshot<StoreOptions<int, int>>>(MockBehavior.Strict);
    private readonly Mock<ISystemClock> mockClock = new Mock<ISystemClock>(MockBehavior.Strict);
    private readonly DateTimeOffset utcNow = new DateTimeOffset(2022, 10, 21, 13, 30, 13, 0, TimeSpan.Zero);

    public DefaultKeyValueStoreTests()
    {
        mockMemFactory.Setup(f => f.Create()).Returns(mockMemStore.Object);
        mockOptions.SetupGet(o => o.Value).Returns(options);
        mockClock.SetupGet(c => c.UtcNow).Returns(utcNow);
    }

    [Theory, AutoData]
    public void TryGetFindsNothingInZeroSegmentsAndEmptyMemoryStore(int key)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockMemStore.Setup(s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny)).Returns(false);

        Assert.False(store.TryGet(key, out _));
    }

    [Theory]
    [InlineData(21, true)]
    [InlineData(42, false)]
    public void TryGetFindsEntryInMemoryStore(int key, bool deletedInMemory)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockMemStore.Setup(s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(new StoreUtils.TryGetCallback<int, int>(
                (int key, out StoreEntry<int, int> value) => value =
                    deletedInMemory ? StoreEntry<int, int>.Delete(key) : new StoreEntry<int, int>(key, key * 7)))
            .Returns(true);

        Assert.Equal(!deletedInMemory, store.TryGet(key, out int value));
        if (!deletedInMemory)
        {
            Assert.Equal(key * 7, value);
        }
    }

    [Theory, AutoData]
    public void SetOverwritesEntryInMemoryStore(int key, int firstValue, int secondValue)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        int? lastValue = null;
        mockMemStore.Setup(s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(new StoreUtils.TryGetCallback<int, int>(
                (int key, out StoreEntry<int, int> value) => value = lastValue.HasValue ?
                    new StoreEntry<int, int>(key, lastValue.Value) :
                    StoreEntry<int, int>.Delete(key)))
            .Returns(true);
        mockMemStore.Setup(s => s.Set(It.Is<StoreEntry<int, int>>(entry =>
            entry.Key == key && !entry.IsDeleted && (entry.Value == firstValue || entry.Value == secondValue))))
            .Callback((StoreEntry<int, int> entry) => lastValue = entry.Value);
        mockMemStore.SetupGet(s => s.Count).Returns(1);

        Assert.False(store.TryGet(key, out _));

        store.Set(key, firstValue);
        Assert.True(store.TryGet(key, out int value));
        Assert.Equal(firstValue, value);

        store.Set(key, secondValue);
        Assert.True(store.TryGet(key, out value));
        Assert.Equal(secondValue, value);
    }

    [Theory, AutoData]
    public void DeleteOverwritesEntryInMemoryStore(int key, int initialValue)
    {
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        int? lastValue = initialValue;
        mockMemStore.Setup(s => s.TryGet(key, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(new StoreUtils.TryGetCallback<int, int>(
                (int key, out StoreEntry<int, int> value) => value = lastValue.HasValue ?
                    new StoreEntry<int, int>(key, lastValue.Value) :
                    StoreEntry<int, int>.Delete(key)))
            .Returns(true);
        mockMemStore.Setup(s => s.Set(It.Is<StoreEntry<int, int>>(entry => entry.Key == key && entry.IsDeleted)))
            .Callback(() => lastValue = null);
        mockMemStore.SetupGet(s => s.Count).Returns(1);

        Assert.True(store.TryGet(key, out int value));
        Assert.Equal(initialValue, value);

        store.Delete(key);
        Assert.False(store.TryGet(key, out value));
    }

    [Theory, AutoData]
    public async Task PersistTaskQueuedWhenPolicyDemandsItButUltimatelySkipped(int key)
    {
        mockMemFactory.SetupSequence(f => f.Create())
            .Returns(mockMemStore.Object)
            .Throws(new Exception("A second store must not be created."));

        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();
        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockMemStore.Setup(s => s.Set(It.Is<StoreEntry<int, int>>(entry => entry.Key == key && entry.IsDeleted)));
        mockMemStore.SetupSequence(s => s.Count).Returns(1).Returns(0 /* because we want to avoid a flush on close */);

        options.Settings.PersistPolicy = new DefaultPersistPolicy(0, TimeSpan.Zero);

        store.Delete(key);

        await store.DisposeAsync();
    }

    [Fact]
    public void PersistTaskQueuedOnClose()
    {
        mockMemFactory.SetupSequence(f => f.Create())
            .Returns(mockMemStore.Object)
            .Returns(mockMemStore.Object)
            .Throws(new Exception("A third store must not be created."));

        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();

        mockSegmentManager.Setup(m => m.CreateNewSegment(0)).Returns((long segmentId) =>
        {
            Mock<IEntryFormatter<int, int>> mockFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Loose);
            WriterContext context = StoreUtils.CreateSegmentWriter(MockBehavior.Loose);

            return StoreUtils.CreateSegment(segmentId, context.Writer.Object, mockFormatter.Object);
        });

        mockMemStore.Setup(s => s.GetOrderedEnumerator()).Returns(fixture.Create<IEnumerator<StoreEntry<int, int>>>());

        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockMemStore.Setup(s => s.Count).Returns(1);

        store.Dispose();

        mockMemStore.Verify(s => s.GetOrderedEnumerator(), Times.Once);
    }

    [Theory, AutoData]
    public async Task PersistAddsNewSegmentToSegmentsAndStartsMerge(
        int key1,
        int expectedValue1,
        int key2,
        int expectedValue2)
    {
        Mock<IMemoryKeyValueStore<int, int>> mockMemStore2 = new Mock<IMemoryKeyValueStore<int, int>>(MockBehavior.Strict);

        mockMemFactory.SetupSequence(f => f.Create())
            .Returns(mockMemStore.Object)
            .Returns(mockMemStore2.Object)
            .Throws(new Exception("A third store must not be created."));

        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager();

        SemaphoreSlim oldStoreValuesRead = new SemaphoreSlim(0, 1);
        mockSegmentManager.Setup(m => m.CreateNewSegment(0)).Returns((long segmentId) =>
        {
            Mock<IEntryFormatter<int, int>> mockFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Loose);
            WriterContext context = StoreUtils.CreateSegmentWriter(MockBehavior.Loose);

            return StoreUtils.CreateSegment(segmentId, context.Writer.Object, mockFormatter.Object);
        });
        mockSegmentManager.Setup(m => m.MakeReadOnly(It.IsAny<Segment<int, int>>()))
            .Callback(() => oldStoreValuesRead.Wait())
            .Returns(
                (Segment<int, int> segment) => StoreUtils.CreateSegment(segment.Id, new StoreEntry<int, int>(-1, -1)));

        SemaphoreSlim waitForPersist = new SemaphoreSlim(0, 1);
        mockMemStore.Setup(s => s.Set(It.Is<StoreEntry<int, int>>(e => e.Key == key1 && e.Value == expectedValue1)));
        mockMemStore.Setup(s => s.GetOrderedEnumerator())
            .Callback(() => waitForPersist.Release())
            .Returns(fixture.Create<IEnumerator<StoreEntry<int, int>>>());
        mockMemStore2.Setup(s => s.Set(It.Is<StoreEntry<int, int>>(e => e.Key == key2 && e.Value == expectedValue2)));

        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        mockMemStore.Setup(s => s.Count).Returns(1);
        mockMemStore2.Setup(s => s.Count).Returns(0 /* we don't want to start another flush */);
        options.Settings.PersistPolicy = new DefaultPersistPolicy(1, TimeSpan.Zero);

        SemaphoreSlim segmentMadeReadOnly = new SemaphoreSlim(0, 1);
        Mock<IMergePolicy> mockMergePolicy = new Mock<IMergePolicy>(MockBehavior.Strict);
        mockMergePolicy.Setup(p => p.ShouldMerge(It.IsAny<long>())).Callback(() => segmentMadeReadOnly.Release()).Returns(true);
        options.Settings.MergePolicy = mockMergePolicy.Object;
        options.Settings.MinimumPersistInterval = TimeSpan.Zero;
        int msAdded = 0;
        mockClock.Setup(c => c.UtcNow).Returns(() => utcNow.AddMilliseconds(Interlocked.Increment(ref msAdded)));

        store.Set(key1, expectedValue1);
        waitForPersist.Wait();
        store.Set(key2, expectedValue2);

        // A TryGet now will first look in mockMemStore2, and if that fails, it will check mockMemStore (being persisted).
        mockMemStore2.Setup(s => s.TryGet(key1, out It.Ref<StoreEntry<int, int>>.IsAny)).Returns(false);
        mockMemStore2.Setup(s => s.TryGet(key2, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(new StoreUtils.TryGetCallback<int, int>(
                (int key, out StoreEntry<int, int> value) => value = new StoreEntry<int, int>(key, expectedValue2)))
            .Returns(true);
        mockMemStore2.Setup(s => s.TryGet(-1, out It.Ref<StoreEntry<int, int>>.IsAny)).Returns(false);
        mockMemStore.Setup(s => s.TryGet(key1, out It.Ref<StoreEntry<int, int>>.IsAny))
            .Callback(new StoreUtils.TryGetCallback<int, int>(
                (int key, out StoreEntry<int, int> value) => value = new StoreEntry<int, int>(key, expectedValue1)))
            .Returns(true);

        Assert.True(store.TryGet(key1, out int actualValue1));
        Assert.Equal(expectedValue1, actualValue1);
        Assert.True(store.TryGet(key2, out int actualValue2));
        Assert.Equal(expectedValue2, actualValue2);
        oldStoreValuesRead.Release();

        segmentMadeReadOnly.Wait();
        await Task.Delay(TimeSpan.FromMilliseconds(10));
        // The next value would be read from the newly added read-only segment.
        Assert.True(store.TryGet(-1, out int actualValueFromSegment));
        Assert.Equal(-1, actualValueFromSegment);

        mockMemStore.Verify(s => s.GetOrderedEnumerator(), Times.Once);
        mockMergePolicy.Verify(p => p.ShouldMerge(It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public async Task MergeWorks()
    {
        // Segment 1: entries for keys 0-4; keys 1 and 3 are deleted; all others have value 1;
        Segment<int, int> seg1 = StoreUtils.CreateSegment(1, Enumerable.Range(0, 5)
            .Select(i => i % 2 == 0 ? new StoreEntry<int, int>(i, 1) : StoreEntry<int, int>.Delete(i))
            .ToArray());
        // Segment 2: entries for keys 0,3,6,9; all have value 2.
        Segment<int, int> seg2 = StoreUtils.CreateSegment(2, Enumerable.Range(0, 4)
            .Select(i => new StoreEntry<int, int>(i * 3, 2)).ToArray());

        Segment<int, int> newSegment = default;
        Mock<IEntryFormatter<int, int>>? mockFormatter = default;
        WriterContext writerContext = default;
        Mock<ISegmentManager<int, int>> mockSegmentManager = StoreUtils.CreateSegmentManager(seg1, seg2);
        mockSegmentManager.Setup(m => m.CreateNewSegment(3))
            .Returns((long segmentId) =>
            {
                mockFormatter = new Mock<IEntryFormatter<int, int>>(MockBehavior.Loose);
                writerContext = StoreUtils.CreateSegmentWriter(MockBehavior.Loose);
                newSegment = StoreUtils.CreateSegment(segmentId, writerContext.Writer.Object, mockFormatter.Object);

                return newSegment;
            });
        mockSegmentManager.Setup(m => m.DeleteSegmentAsync(It.IsIn<long>(1, 2), default)).Returns(new ValueTask());

        SemaphoreSlim newSegmentMadeReadOnly = new SemaphoreSlim(0, 1);
        mockSegmentManager.Setup(m => m.MakeReadOnly(It.Is<Segment<int, int>>(
                seg => seg.Id == newSegment.Id && seg.Driver == seg.Driver)))
            .Returns(() =>
            {
                newSegmentMadeReadOnly.Release();
                return newSegment;
            });

        DefaultKeyValueStore<int, int> store = new DefaultKeyValueStore<int, int>(
            NullLogger<DefaultKeyValueStore<int, int>>.Instance,
            mockMemFactory.Object,
            mockSegmentManager.Object,
            mockOptions.Object,
            mockClock.Object);

        store.Merge();

        newSegmentMadeReadOnly.Wait();
        await Task.Delay(TimeSpan.FromMilliseconds(1));
        Assert.NotNull(mockFormatter);
        Assert.NotNull(writerContext.IndexStream);
        Assert.NotNull(writerContext.DataStream);
        // The merged sequence should be (key,value): (0,2),(2,1),(3,2),(4,1),(6,2),(9,2) -- 6 entries total.
        mockFormatter.Verify(f => f.WriteKeyAsync(0, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(2, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(3, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(4, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(6, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(9, writerContext.DataStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteValueAsync(1, writerContext.DataStream, default), Times.Exactly(2));
        mockFormatter.Verify(f => f.WriteValueAsync(2, writerContext.DataStream, default), Times.Exactly(4));
        // The index will only have the first and last entries' keys: 0 and 9.
        mockFormatter.Verify(f => f.WriteKeyAsync(0, writerContext.IndexStream, default), Times.Once);
        mockFormatter.Verify(f => f.WriteKeyAsync(9, writerContext.IndexStream, default), Times.Once);
    }
}
