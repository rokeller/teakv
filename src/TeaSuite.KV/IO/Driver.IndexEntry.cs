namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Defines an entry in a segment's index.
    /// </summary>
    internal readonly record struct IndexEntry
    {
        /// <summary>
        /// Gets the (0-based) position of the entry in the index.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the key of the entry referred to in the index.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Gets the byte-offset of the entry in the segment's data file.
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="IndexEntry"/>.
        /// </summary>
        /// <param name="id">
        /// The (0-based) position of the entry in the index.
        /// </param>
        /// <param name="key">
        /// The key of the entry referred to in the index.
        /// </param>
        /// <param name="position">
        /// The byte-offset of the entry in the segment's data file.
        /// </param>
        public IndexEntry(int id, TKey key, long position)
        {
            Id = id;
            Key = key;
            Position = position;
        }
    }
}
