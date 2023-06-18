using System;
using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV;

/// <summary>
/// Implements an <see cref="IEnumerator{T}"/> that converts items of type
/// <typeparamref name="TIn"/> into items of type <typeparamref name="TOut"/>
/// as they are retrieved.
/// </summary>
/// <typeparam name="TIn">
/// The type of the items that is enumerated from.
/// </typeparam>
/// <typeparam name="TOut">
/// The type of the items that is to be enumerated to.
/// </typeparam>
internal readonly struct TransformingEnumerator<TIn, TOut> : IEnumerator<TOut>
{
    private readonly IEnumerator<TIn> input;
    private readonly Func<TIn, TOut> transform;

    /// <summary>
    /// Initializes a new instance of <see cref="TransformingEnumerator{TIn, TOut}"/>.
    /// </summary>
    /// <param name="input">
    /// The inner <see cref="IEnumerator{T}"/> of <typeparamref name="TIn"/> to
    /// take the input items from.
    /// </param>
    /// <param name="transform">
    /// A function that transforms items of type <typeparamref name="TIn"/> into
    /// items of type <typeparamref name="TOut"/>.
    /// </param>
    public TransformingEnumerator(IEnumerator<TIn> input, Func<TIn, TOut> transform)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (transform is null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        this.input = input;
        this.transform = transform;
    }

    /// <inheritdoc/>
    public TOut Current => transform(input.Current);

    /// <inheritdoc/>
    object IEnumerator.Current => Current!;

    /// <inheritdoc/>
    public void Dispose()
    {
        input.Dispose();
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        return input.MoveNext();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        input.Reset();
    }
}
