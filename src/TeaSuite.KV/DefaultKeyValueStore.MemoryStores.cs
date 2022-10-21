using TeaSuite.KV.Data;

namespace TeaSuite.KV;

partial class DefaultKeyValueStore<TKey, TValue>
{
    /// <summary>
    /// Tracks current and old <see cref="IMemoryKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    private sealed class MemoryStores
    {
        public MemoryStores(IMemoryKeyValueStore<TKey, TValue> current)
        {
            Current = current;
            Old = null;
        }

        public MemoryStores(IMemoryKeyValueStore<TKey, TValue> current, MemoryStores previous)
        {
            Current = current;
            Old = previous.Current;
        }

        public IMemoryKeyValueStore<TKey, TValue> Current { get; }

        public IMemoryKeyValueStore<TKey, TValue>? Old { get; }
    }
}
