using System;
using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Implements an <see cref="IEnumerator{T}"/> that stops whenever the configured
/// upper bound is reached.
/// </summary>
internal readonly struct UpperBoundEnumerator<T> :
    IEnumerator<T>
    where T : IComparable<T>
{
    private readonly IEnumerator<T> inner;
    private readonly T upperBound;

    /// <summary>
    /// Initializes a new instance of <see cref="UpperBoundEnumerator{T}"/>.
    /// </summary>
    /// <param name="inner">
    /// The inner <see cref="IEnumerator{T}"/>, which is assumed to be sorted.
    /// </param>
    /// <param name="upperBound">
    /// The upper bound value that, when reached, causes this enumerator to stop.
    /// </param>
    public UpperBoundEnumerator(IEnumerator<T> inner, T upperBound)
    {
        if (inner is null)
        {
            throw new ArgumentNullException(nameof(inner));
        }

        this.inner = inner;
        this.upperBound = upperBound;
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
        bool canMove = inner.MoveNext();

        // Continue only when the current item is less than the upper bound.
        return canMove && inner.Current.CompareTo(upperBound) < 0;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        inner.Reset();
    }
}
