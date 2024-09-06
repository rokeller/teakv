using System;
using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Implements <see cref="IEnumerator{T}"/> with a guard that is automatically
/// disposed when the enumerator is disposed.
/// </summary>
/// <typeparam name="T">
/// The type of the values to enumerate.
/// </typeparam>
/// <param name="Guard">
/// The guarded <see cref="IDisposable"/> to dispose when the enumerator is
/// disposed.
/// </param>
/// <param name="Inner">
/// The inner <see cref="IEnumerator{T}"/> to be guarded.
/// </param>
public readonly record struct GuardingEnumerator<T>(
    IDisposable Guard,
    IEnumerator<T> Inner) : IEnumerator<T>
{
    /// <inheritdoc/>
    public T Current => Inner.Current;

    /// <inheritdoc/>
    object IEnumerator.Current => ((IEnumerator)Inner).Current;

    /// <inheritdoc/>
    public void Dispose()
    {
        Inner.Dispose();
        Guard.Dispose();
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        return Inner.MoveNext();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        Inner.Reset();
    }
}
