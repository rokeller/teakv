using System.Collections;
using AutoFixture.Xunit2;

namespace TeaSuite.KV;

public sealed class UpperBoundEnumeratorTests
{
    [Fact]
    public void CtorValidatesInput()
    {
        Assert.Throws<ArgumentNullException>(
            "inner",
            () => new UpperBoundEnumerator<int>(null!, 0));
    }

    [Theory, AutoData]
    public void UpperBoundIsRespected(int bound)
    {
        bound = 1 + Math.Abs(bound);
        int numItems = 0;
        IEnumerable<int> values = Enumerable.Range(0, bound).Select(x => x * 2);

        using UpperBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);
        int lastLowerBound = 0;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound);
            Assert.Equal(enumerator.Current, ((IEnumerator)enumerator).Current);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        Assert.Equal(bound / 2 + (bound % 2), numItems);
    }

    [Theory, AutoData]
    public void UpperBoundRemainsUnreached(int bound)
    {
        bound = 2 + Math.Abs(bound);
        int numItems = 0;
        IEnumerable<int> values = Enumerable.Range(0, bound - 1);

        using UpperBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);
        int lastLowerBound = 0;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        Assert.Equal(bound - 1, numItems);
    }

    [Theory, AutoData]
    public void ResetWorks(int bound)
    {
        bound = 1 + Math.Abs(bound);
        int numItems = 0;
        List<int> values = Enumerable.Range(0, bound).Select(x => x * 2).ToList();

        using UpperBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);
        int lastLowerBound = 0;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        enumerator.Reset();
        lastLowerBound = 0;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        Assert.Equal((bound >> 1 << 1) + (bound % 2) * 2, numItems);
    }
}
