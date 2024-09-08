using System.Collections;

namespace TeaSuite.KV;

public sealed class FilteringEnumeratorTests
{
    private readonly Fixture fixture = new();

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    public void EnumeratorWorks(int numItems)
    {
        using FilteringEnumerator<int> filtering = new(
            Enumerable.Range(0, numItems).GetEnumerator(), KeepEven);

        while (filtering.MoveNext())
        {
            Assert.True(filtering.Current % 2 == 0);
            Assert.Equal(filtering.Current, ((IEnumerator)filtering).Current);
        }
    }

    private static bool KeepEven(int i)
    {
        return i % 2 == 0;
    }
}
