namespace TeaSuite.KV.Data;

public sealed class AvlTreeTests
{
    private readonly Fixture fixture = new();
    private readonly AvlTree<int> tree = new(Comparer<int>.Default);

    [Fact]
    public void TestRandomTree()
    {
        const int NumItems = 5000;
        HashSet<int> unique = new(NumItems);
        for (int i = 0; i < NumItems; i++)
        {
            int random = fixture.Create<int>();
            tree.Upsert(random);
            unique.Add(random);
        }

        int? lastValue = null;
        using IEnumerator<int> enumerator = tree.GetInOrderEnumerator();

        while (enumerator.MoveNext())
        {
            Assert.Contains(enumerator.Current, unique);

            if (lastValue.HasValue)
            {
                Assert.True(lastValue.Value < enumerator.Current);
            }

            Assert.True(unique.Remove(enumerator.Current));
            lastValue = enumerator.Current;

            Assert.True(tree.TryFind(enumerator.Current, out int value));
            Assert.Equal(enumerator.Current, value);
        }

        Assert.Empty(unique);
    }

    [Fact]
    public void UpsertWorks()
    {
        tree.Upsert(0).Upsert(2).Upsert(4).Upsert(6).Upsert(8);
        tree.Upsert(9).Upsert(7).Upsert(5).Upsert(3).Upsert(1);

        List<int> values = new(tree.Count);
        using IEnumerator<int> enumerator = tree.GetInOrderEnumerator();
        while (enumerator.MoveNext())
        {
            values.Add(enumerator.Current);
        }

        Assert.Collection(values,
            i => Assert.Equal(0, i),
            i => Assert.Equal(1, i),
            i => Assert.Equal(2, i),
            i => Assert.Equal(3, i),
            i => Assert.Equal(4, i),
            i => Assert.Equal(5, i),
            i => Assert.Equal(6, i),
            i => Assert.Equal(7, i),
            i => Assert.Equal(8, i),
            i => Assert.Equal(9, i)
            );
    }

    [Fact]
    public void TryFindWorks()
    {
        int found;
        Assert.False(tree.TryFind(1, out found));

        tree.Upsert(5);
        Assert.False(tree.TryFind(1, out found));
        Assert.Equal(1, tree.Count);

        tree.Upsert(1);
        Assert.True(tree.TryFind(1, out found));
        Assert.Equal(1, found);
        Assert.Equal(2, tree.Count);

        tree.Upsert(1);
        Assert.True(tree.TryFind(1, out found));
        Assert.Equal(1, found);
        Assert.Equal(2, tree.Count);
    }
}
