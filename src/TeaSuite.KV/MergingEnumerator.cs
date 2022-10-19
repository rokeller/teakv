using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TeaSuite.KV;

/// <summary>
/// Implements <see cref="IEnumerator{T}"/> to merge items for multiple sorted input enumerators.
/// </summary>
/// <typeparam name="T">
/// The type of the items that are to be merged.
/// </typeparam>
internal sealed class MergingEnumerator<T> : IEnumerator<T> where T : IComparable<T>
{
    /// <summary>
    /// Tracks the remaining enumerators which still have items left to be enumerated.
    /// </summary>
    private ImmutableList<IEnumerator<T>> enumerators;

    /// <summary>
    /// A flag which indicates whether enumeration has not yet begun.
    /// </summary>
    private bool isAtBeginning = true;

    /// <summary>
    /// The current item to be returned by the enumerator.
    /// </summary>
    private T? current = default;

    /// <summary>
    /// Initializes a new instance of MergingEnumerator.
    /// </summary>
    /// <param name="enumerators">
    /// The <see cref="IEnumerator{T}"/> instances with sorted items to be merged.
    /// </param>
    public MergingEnumerator(params IEnumerator<T>[] enumerators)
    {
        this.enumerators = ImmutableList.CreateRange<IEnumerator<T>>(enumerators);
    }

    /// <inheritdoc/>
    public T Current => current!;

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (IEnumerator<T> enumerator in enumerators)
        {
            enumerator.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (isAtBeginning)
        {
            // Position each enumerator at the first element.
            enumerators = MoveNext(enumerators);
            isAtBeginning = false;
        }

        // We can move next only as long as we have enumerators with items left.
        if (enumerators.Count > 0)
        {
            current = GetNextValue();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the next higher value from the remaining enumerators.
    /// </summary>
    /// <returns>
    /// A value of <typeparamref name="T"/> representing the next value in order.
    /// </returns>
    private T GetNextValue()
    {
        // Track the current candidate for the next value.
        T minValue = enumerators[0].Current;
        ImmutableHashSet<int> matchingEnumeratorIds = ImmutableHashSet.Create(0);

        for (int i = 1; i < enumerators.Count; i++)
        {
            int result = enumerators[i].Current.CompareTo(minValue);
            if (result < 0)
            {
                // We found an item that comes sooner in the order.
                matchingEnumeratorIds = ImmutableHashSet.Create(i);
                minValue = enumerators[i].Current;
            }
            else if (result == 0)
            {
                // We found an item that has the same position in the sort order. Make sure we advance this enumerator
                // too when the candidate is ultimately chosen.
                matchingEnumeratorIds = matchingEnumeratorIds.Add(i);
            }
        }

        enumerators = MoveNext(enumerators, matchingEnumeratorIds.Contains);

        return minValue;
    }

    /// <summary>
    /// Advances all the given <paramref name="enumerators"/> to the next item.
    /// </summary>
    /// <param name="enumerators">
    /// The <see cref="IEnumerator{T}"/> instances to advance.
    /// </param>
    /// <returns>
    /// An <see cref="ImmutableList{T}"/> of <see cref="IEnumerator{T}"/> defining all the remaining enumerators that
    /// still have items left to be enumerated.
    /// </returns>
    private static ImmutableList<IEnumerator<T>> MoveNext(ImmutableList<IEnumerator<T>> enumerators)
    {
        static bool Always(int index)
        {
            return true;
        }

        return MoveNext(enumerators, Always);
    }

    /// <summary>
    /// Advances all the <paramref name="enumerators"/> matching the predicate <paramref name="shouldMoveNext"/> to the
    /// next item.
    /// </summary>
    /// <param name="enumerators">
    /// The <see cref="IEnumerator{T}"/> instances to advance.
    /// </param>
    /// <param name="shouldMoveNext">
    /// A predicate that defines if enumerator at a given position should move to the next item.
    /// </param>
    /// <returns>
    /// An <see cref="ImmutableList{T}"/> of <see cref="IEnumerator{T}"/> defining all the remaining enumerators that
    /// still have items left to be enumerated.
    /// </returns>
    private static ImmutableList<IEnumerator<T>> MoveNext(
        ImmutableList<IEnumerator<T>> enumerators,
        Func<int, bool> shouldMoveNext)
    {
        int index = 0;
        foreach (IEnumerator<T> enumerator in enumerators)
        {
            if (!shouldMoveNext(index++))
            {
                continue;
            }

            if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                enumerators = enumerators.Remove(enumerator);
            }
        }

        return enumerators;
    }
}
