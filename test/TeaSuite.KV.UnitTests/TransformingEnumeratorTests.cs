using System.Collections;
using AutoFixture.Xunit2;

namespace TeaSuite.KV;

public sealed class TransformingEnumeratorTests
{
    [Fact]
    public void CtorValidatesInput()
    {
        Assert.Throws<ArgumentNullException>(
            "input",
            () => new TransformingEnumerator<int, int>(null!, null!));

        Assert.Throws<ArgumentNullException>(
            "transform",
            () => new TransformingEnumerator<int, int>(
                Enumerable.Empty<int>().GetEnumerator(), null!));
    }

    [Theory, AutoData]
    public void TransformingWorks(List<int> input)
    {
        int transformed = 0;
        static long Transform(int input)
        {
            return (long)input + 2;
        }

        using TransformingEnumerator<int, long> enumerator =
            new(input.GetEnumerator(), Transform);

        while (enumerator.MoveNext())
        {
            long expected = (long)input[transformed] + 2L;
            Assert.Equal(expected, enumerator.Current);
            Assert.Equal(expected, ((IEnumerator)enumerator).Current);

            transformed++;
        }

        Assert.Equal(transformed, input.Count);
    }

    [Theory, AutoData]
    public void ResetWorks(List<int> input)
    {
        int transformed = 0;
        static long Transform(int input)
        {
            return (long)input + 2;
        }

        using TransformingEnumerator<int, long> enumerator =
            new(input.GetEnumerator(), Transform);

        while (enumerator.MoveNext())
        {
            long expected = (long)input[transformed] + 2L;
            Assert.Equal(expected, enumerator.Current);
            Assert.Equal(expected, ((IEnumerator)enumerator).Current);

            transformed++;
        }

        enumerator.Reset();

        while (enumerator.MoveNext())
        {
            long expected = (long)input[transformed - input.Count] + 2L;
            Assert.Equal(expected, enumerator.Current);
            Assert.Equal(expected, ((IEnumerator)enumerator).Current);

            transformed++;
        }

        Assert.Equal(transformed, 2 * input.Count);
    }
}
