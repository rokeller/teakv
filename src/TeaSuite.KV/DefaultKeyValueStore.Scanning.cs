using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TeaSuite.KV;

partial class DefaultKeyValueStore<TKey, TValue>
{
    /// <inheritdoc/>
    protected override IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> GetEntriesEnumerators(
        Range<TKey> range)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override IEnumerator<StoreEntry<TKey, TValue>> CreateEntriesEnumerator(
        IEnumerable<IEnumerator<StoreEntry<TKey, TValue>>> enumerators)
    {
        throw new NotImplementedException();
    }
}
