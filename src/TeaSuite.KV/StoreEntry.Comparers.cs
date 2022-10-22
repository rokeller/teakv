using System.Collections.Generic;

namespace TeaSuite.KV;

partial struct StoreEntry<TKey, TValue>
{
    /// <summary>
    /// The <see cref="IComparer{T}"/> to use when comparing only keys.
    /// </summary>
    public static IComparer<StoreEntry<TKey, TValue>> KeyComparer => new KeyComparerImpl();

    /// <summary>
    /// Implements <see cref="IComparer{T}"/> that compares only entries' keys.
    /// </summary>
    private readonly struct KeyComparerImpl : IComparer<StoreEntry<TKey, TValue>>
    {
        /// <inheritdoc/>
        public int Compare(StoreEntry<TKey, TValue> x, StoreEntry<TKey, TValue> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
