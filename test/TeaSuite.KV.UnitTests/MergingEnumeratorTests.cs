using System.Collections;
using System.Security.Cryptography;
using Moq;

namespace TeaSuite.KV;

public sealed class MergingEnumeratorTests
{
    private readonly Fixture fixture = new();

    [Fact]
    public void EnumeratorProducesOrderedResults()
    {
        fixture.Register<IEnumerable<int>>(
            () => GeneratedSortedEnumerable(fixture.Create<byte>()));
        IEnumerable<int> first = fixture.Create<IEnumerable<int>>();
        IEnumerable<int> second = fixture.Create<IEnumerable<int>>();
        IEnumerable<int> third = fixture.Create<IEnumerable<int>>();
        IEnumerator<int> union = first
            .Union(second)
            .Union(third)
            .OrderBy(i => i).GetEnumerator();

        MergingEnumerator<int> enumerator = new(
            first.GetEnumerator(), second.GetEnumerator(), third.GetEnumerator());
        int? prevValue = null;

        while (enumerator.MoveNext())
        {
            Assert.True(union.MoveNext());

            if (prevValue.HasValue)
            {
                Assert.True(prevValue.Value < enumerator.Current);
            }

            prevValue = enumerator.Current;
            Assert.Equal(union.Current, enumerator.Current);
            Assert.Equal(union.Current, ((IEnumerator)enumerator).Current);
        }

        Assert.False(union.MoveNext());
    }

    [Fact]
    public void SingleInputEnumeratorWorks()
    {
        fixture.Register<IEnumerable<int>>(
            () => GeneratedSortedEnumerable(fixture.Create<byte>()));
        IEnumerable<int> first = fixture.Create<IEnumerable<int>>();
        IEnumerator<int> union = first.GetEnumerator();

        MergingEnumerator<int> enumerator = new(first.GetEnumerator());
        int? prevValue = null;

        while (enumerator.MoveNext())
        {
            Assert.True(union.MoveNext());

            if (prevValue.HasValue)
            {
                Assert.True(prevValue.Value < enumerator.Current);
            }

            prevValue = enumerator.Current;
            Assert.Equal(union.Current, enumerator.Current);
        }

        Assert.False(union.MoveNext());
    }

    [Fact]
    public void EmptyInputEnumeratorsWorks()
    {
        MergingEnumerator<int> enumerator = new();

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void DisposeDisposesEnumerators()
    {
        Mock<IEnumerator<int>> mockEnum1 = new(MockBehavior.Strict);
        Mock<IEnumerator<int>> mockEnum2 = new(MockBehavior.Strict);
        MergingEnumerator<int> enumerator = new(mockEnum1.Object, mockEnum2.Object);

        mockEnum1.Setup(e => e.Dispose());
        mockEnum2.Setup(e => e.Dispose());

        enumerator.Dispose();

        mockEnum1.Verify(e => e.Dispose(), Times.Once);
        mockEnum2.Verify(e => e.Dispose(), Times.Once);
    }

    [Fact]
    public void ResetThrowsNotSupportedException()
    {
        MergingEnumerator<int> enumerator = fixture.Create<MergingEnumerator<int>>();

        Assert.Throws<NotSupportedException>(() => enumerator.Reset());
    }

    private IEnumerable<int> GeneratedSortedEnumerable(byte size)
    {
        int n = 1 + size;
        List<int> items = new(n);

        for (int i = 0; i < n; i++)
        {
            items.Add(RandomNumberGenerator.GetInt32(100));
        }

        items.Sort();
        return items.Distinct();
    }
}
