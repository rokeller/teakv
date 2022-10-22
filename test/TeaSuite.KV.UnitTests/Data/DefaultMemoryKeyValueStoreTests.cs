namespace TeaSuite.KV.Data;

public sealed class DefaultMemoryKeyValueStoreTests
{
    private readonly DefaultMemoryKeyValueStore<int, int> store = new DefaultMemoryKeyValueStore<int, int>();

    [Fact]
    public void SetWorks()
    {
        StoreEntry<int, int> entry;
        Assert.False(store.TryGet(1, out entry));

        store.Set(new StoreEntry<int, int>(1, 2));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.Equal(2, entry.Value);
        Assert.False(entry.IsDeleted);

        store.Set(StoreEntry<int, int>.Delete(1));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.True(entry.IsDeleted);

        store.Set(new StoreEntry<int, int>(1, 3));
        Assert.True(store.TryGet(1, out entry));
        Assert.Equal(1, entry.Key);
        Assert.Equal(3, entry.Value);
        Assert.False(entry.IsDeleted);
    }

    [Fact]
    public void SetThrowsInvalidOperationExceptionAfterEnumeratorRequested()
    {
        store.Set(new StoreEntry<int, int>(1, 1));
        store.GetOrderedEnumerator();
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => store.Set(new StoreEntry<int, int>(1, 1)));

        Assert.Equal("Cannot write to the store: the store is read-only.", ex.Message);
    }

    [Fact]
    public void GetOrderedEnumeratorWorks()
    {
        store.Set(new StoreEntry<int, int>(-1, -2));
        store.Set(new StoreEntry<int, int>(3, 4));
        store.Set(new StoreEntry<int, int>(1, 2));
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
            entry => Assert.Equal(entry, new StoreEntry<int, int>(-1, -2)),
            entry => Assert.Equal(entry, new StoreEntry<int, int>(1, 2)),
            entry => Assert.Equal(entry, new StoreEntry<int, int>(3, 4))
            );
    }

    [Fact]
    public void GetOrderedEnumeratorThrowsOnSecondCall()
    {
        store.GetOrderedEnumerator();
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => store.GetOrderedEnumerator());

        Assert.Equal("An ordered enumerator has already been requested before.", ex.Message);
    }
}
