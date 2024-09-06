using System.Collections;
using AutoFixture.Xunit2;
using Moq;
using Moq.Language;

namespace TeaSuite.KV;

public sealed class GuardingEnumeratorTests
{
    private readonly Mock<IDisposable> mockDisposable = new(MockBehavior.Strict);
    private readonly Mock<IEnumerator<uint>> mockInner = new(MockBehavior.Strict);
    private readonly GuardingEnumerator<uint> enumerator;

    public GuardingEnumeratorTests()
    {
        enumerator = new(mockDisposable.Object, mockInner.Object);
    }

    [Theory]
    [InlineAutoData]
    public void EnumeratorForwardsMoveNextAndCurrentToInner(uint numItems)
    {
        ISetupSequentialResult<bool> seqNext = mockInner.SetupSequence(e => e.MoveNext());
        ISetupSequentialResult<uint> seqCur = mockInner.SetupSequence(e => e.Current);
        for (uint i = 0; i < numItems; i++)
        {
            seqNext = seqNext.Returns(true);
            seqCur = seqCur.Returns(i);
        }
        seqNext.Returns(false);
        seqCur.Throws(new InvalidOperationException());

        for (uint i = 0; i < numItems; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }

        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        mockInner.Verify(e => e.MoveNext(), Times.Exactly((int)numItems + 1));
        mockInner.Verify(e => e.Current, Times.Exactly((int)numItems + 1));
    }

    [Fact]
    public void ResetForwardsToInner()
    {
        mockInner.Setup(e => e.Reset());

        enumerator.Reset();

        mockInner.Verify(e => e.Reset(), Times.Once);
    }

    [Fact]
    public void IEnumeratorCurrentForwardsToInner()
    {
        mockInner.As<IEnumerator>().Setup(e => e.Current).Returns(123);

        Assert.Equal(123, ((IEnumerator)enumerator).Current);

        mockInner.As<IEnumerator>().Verify(e => e.Current, Times.Once);
    }

    [Fact]
    public void DisposeDisposesGuardAndInner()
    {
        mockInner.Setup(e => e.Dispose());
        mockDisposable.Setup(d => d.Dispose());

        enumerator.Dispose();

        mockInner.Verify(e => e.Dispose(), Times.Once);
        mockDisposable.Verify(d => d.Dispose(), Times.Once);
    }
}