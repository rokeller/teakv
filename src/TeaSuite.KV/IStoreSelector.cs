using System;
namespace TeaSuite.KV;

public interface IStoreSelector<TKey, TValue, TSelectorKey> where TKey : IComparable<TKey>
{
    IReadOnlyKeyValueStore<TKey, TValue> Select(TSelectorKey selectorKey);
}
