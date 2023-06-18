using System;
using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Implements an <see cref="IEnumerator{T}"/> that skips items as long as the
/// configured lower bound is not reached yet.
/// </summary>
internal readonly struct LowerBoundEnumerator<T> :
    IEnumerator<T>
    where T : IComparable<T>
{
    private readonly IEnumerator<T> inner;
    private readonly T lowerBound;

    /// <summary>
    /// Initializes a new instance of <see cref="LowerBoundEnumerator{T}".
    /// </summary>
    /// <param name="inner">
    /// The inner <see cref="IEnumerator{T}"/>, which is assumed to be sorted.
    /// </param>
    /// <param name="lowerBound">
    /// The lower bound value that, before reached, causes this enumerator to
    /// skip items.
    /// </param>
    public LowerBoundEnumerator(IEnumerator<T> inner, T lowerBound)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        this.inner = inner;
        this.lowerBound = lowerBound;
    }

    /// <inheritdoc/>
    public T Current => inner.Current;

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public void Dispose()
    {
        inner.Dispose();
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        bool didMove = inner.MoveNext();

        while (didMove && inner.Current.CompareTo(lowerBound) < 0)
        {
            didMove = inner.MoveNext();
        }

        return didMove;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        inner.Reset();
    }
}
