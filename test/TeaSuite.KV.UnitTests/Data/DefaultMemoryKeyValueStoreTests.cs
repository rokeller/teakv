namespace TeaSuite.KV.Data;

public sealed class DefaultMemoryKeyValueStoreTests
{
    private readonly DefaultMemoryKeyValueStore<int, int> store = new();

    [Fact]
    public void SetWorks()
    {
        StoreEntry<int, int> entry;
        Assert.False(store.TryGet(1, out entry));

        store.Set(new(1, 2));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.Equal(2, entry.Value);
        Assert.False(entry.IsDeleted);

        store.Set(StoreEntry<int, int>.Delete(1));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.True(entry.IsDeleted);

        store.Set(new(1, 3));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.Equal(3, entry.Value);
        Assert.False(entry.IsDeleted);
    }

    [Fact]
    public void SetThrowsInvalidOperationExceptionAfterEnumeratorRequested()
    {
        store.Set(new(1, 1));
        store.GetOrderedEnumerator();
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => store.Set(new(1, 1)));

        Assert.Equal("Cannot write to the store: the store is read-only.", ex.Message);
    }

    [Fact]
    public void SetWorksAgainAfterEnumeratorDisposed()
    {
        StoreEntry<int, int> entry;
        store.Set(new(1, 1));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Value);

        using (store.GetOrderedEnumerator())
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => store.Set(new(1, 2)));
            Assert.Equal(
                "Cannot write to the store: the store is read-only.", ex.Message);
        }

        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Value);

        store.Set(new(1, 3));

        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(3, entry.Value);
    }

    [Fact]
    public void GetOrderedEnumeratorWorks()
    {
        store.Set(new(-1, -2));
        store.Set(new(3, 4));
        store.Set(new(1, 2));
        store.Set(StoreEntry<int, int>.Delete(-3));

        List<StoreEntry<int, int>> orderedItems = new(store.Count);
        using (IEnumerator<StoreEntry<int, int>> enumerator = store.GetOrderedEnumerator())
        {
            while (enumerator.MoveNext())
            {
                orderedItems.Add(enumerator.Current);
            }
        }

        Assert.Collection(orderedItems,
            entry => Assert.Equal(entry, StoreEntry<int, int>.Delete(-3)),
            entry => Assert.Equal(entry, new(-1, -2)),
            entry => Assert.Equal(entry, new(1, 2)),
            entry => Assert.Equal(entry, new(3, 4))
            );
    }

    [Fact]
    public void GetOrderedEnumeratorWithCompleteRangeWorks()
    {
        for (int i = 0; i < 100; i++)
        {
            store.Set(new(i, i + 1));
        }

        List<StoreEntry<int, int>> orderedItems = new(store.Count);
        Range<int> range = new()
        {
            HasStart = true,
            Start = 23,
            HasEnd = true,
            End = 42,
        };

        using (IEnumerator<StoreEntry<int, int>> enumerator = store.GetOrderedEnumerator(range))
        {
            while (enumerator.MoveNext())
            {
                orderedItems.Add(enumerator.Current);
            }
        }

        Assert.Collection(orderedItems,
            Enumerable.Range(23, 42 - 23)
                .Select(i => (Action<StoreEntry<int, int>>)(
                    (entry) => Assert.Equal(new(i, i + 1), entry)))
                .ToArray());
        Assert.Equal(new(41, 42), orderedItems[orderedItems.Count - 1]);
    }

    [Fact]
    public void GetOrderedEnumeratorWithStartOnlyRangeWorks()
    {
        for (int i = 0; i < 100; i++)
        {
            store.Set(new(i, i + 1));
        }

        List<StoreEntry<int, int>> orderedItems = new(store.Count);
        Range<int> range = new()
        {
            HasStart = true,
            Start = 23,
        };

        using (IEnumerator<StoreEntry<int, int>> enumerator = store.GetOrderedEnumerator(range))
        {
            while (enumerator.MoveNext())
            {
                orderedItems.Add(enumerator.Current);
            }
        }

        Assert.Collection(orderedItems,
            Enumerable.Range(23, 100 - 23)
                .Select(i => (Action<StoreEntry<int, int>>)(
                    (entry) => Assert.Equal(new(i, i + 1), entry)))
                .ToArray());
        Assert.Equal(new(99, 100), orderedItems[orderedItems.Count - 1]);
    }

    [Fact]
    public void GetOrderedEnumeratorWithEndOnlyRangeWorks()
    {
        for (int i = 0; i < 100; i++)
        {
            store.Set(new(i, i + 1));
        }

        List<StoreEntry<int, int>> orderedItems = new(store.Count);
        Range<int> range = new()
        {
            HasEnd = true,
            End = 42,
        };

        using (IEnumerator<StoreEntry<int, int>> enumerator = store.GetOrderedEnumerator(range))
        {
            while (enumerator.MoveNext())
            {
                orderedItems.Add(enumerator.Current);
            }
        }

        Assert.Collection(orderedItems,
            Enumerable.Range(0, 42)
                .Select(i => (Action<StoreEntry<int, int>>)(
                    (entry) => Assert.Equal(new(i, i + 1), entry)))
                .ToArray());
        Assert.Equal(new(41, 42), orderedItems[orderedItems.Count - 1]);
    }
}
