using System.Collections;
using AutoFixture.Xunit2;

namespace TeaSuite.KV;

public sealed class LowerBoundEnumeratorTests
{
    [Fact]
    public void CtorValidatesInput()
    {
        Assert.Throws<ArgumentNullException>(
            "inner",
            () => new LowerBoundEnumerator<int>(null!, 0));
    }

    [Theory, AutoData]
    public void LowerBoundIsRespected(int bound)
    {
        bound = 2 + Math.Abs(bound);
        int numItems = 0;
        IEnumerable<int> values = Enumerable.Range(1, bound + 1);

        using LowerBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);
        int lastLowerBound = bound;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound + 1);
            Assert.Equal(enumerator.Current, ((IEnumerator)enumerator).Current);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        Assert.Equal(2, numItems);
    }

    [Theory, AutoData]
    public void LowerBoundRemainsUnreached(int bound)
    {
        bound = 1 + Math.Abs(bound);
        IEnumerable<int> values = Enumerable.Range(0, bound);

        using LowerBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);

        Assert.False(enumerator.MoveNext());
    }

    [Theory, AutoData]
    public void ResetWorks(int bound)
    {
        bound = 2 + Math.Abs(bound);
        int numItems = 0;
        List<int> values = Enumerable.Range(1, bound + 1).ToList();

        using LowerBoundEnumerator<int> enumerator = new(
            values.GetEnumerator(), bound);
        int lastLowerBound = bound;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound + 1);
            Assert.Equal(enumerator.Current, ((IEnumerator)enumerator).Current);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        enumerator.Reset();
        lastLowerBound = bound;

        while (enumerator.MoveNext())
        {
            Assert.InRange(enumerator.Current, lastLowerBound, bound + 1);
            Assert.Equal(enumerator.Current, ((IEnumerator)enumerator).Current);
            lastLowerBound = enumerator.Current + 1;
            numItems++;
        }

        Assert.Equal(4, numItems);
    }
}
