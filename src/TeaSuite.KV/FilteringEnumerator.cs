using System;
using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Implements <see cref="IEnumerator{T}"/> by filtering certain items from an inner <see cref="IEnumerator{T}"/>.
/// </summary>
internal readonly struct FilteringEnumerator<T> : IEnumerator<T>
{
    private readonly IEnumerator<T> inner;
    private readonly Func<T, bool> predicate;

    /// <summary>
    /// Initializes a new instance of <see cref="FilteringEnumerator{T}"/>.
    /// </summary>
    /// <param name="inner">
    /// The <see cref="IEnumerator{T}"/> to use as the source for items to enumerate.
    /// </param>
    /// <param name="predicate">
    /// A <see cref="Func{T, TResult}"/> which is used to decide which items to
    /// keep. This <paramref name="predicate"/> must return <c>true</c> for items
    /// that should be kept and <c>false</c> for all other items.
    /// </param>
    public FilteringEnumerator(IEnumerator<T> inner, Func<T, bool> predicate)
    {
        this.inner = inner;
        this.predicate = predicate;
    }

    /// <inheritdoc/>
    public T Current => inner.Current;

    /// <inheritdoc/>
    object IEnumerator.Current => Current!;

    /// <inheritdoc/>
    public void Dispose()
    {
        inner.Dispose();
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (!inner.MoveNext())
        {
            return false;
        }

        while (!predicate(inner.Current))
        {
            if (!inner.MoveNext())
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        inner.Reset();
    }
}
