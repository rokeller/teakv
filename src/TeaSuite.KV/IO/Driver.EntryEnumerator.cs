using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// that is always empty.
    /// </summary>
    /// <returns>
    /// An empty <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>.
    /// </returns>
    public static IEnumerator<StoreEntry<TKey, TValue>> GetEmptyEnumerator()
    {
        yield break;
    }

    /// <summary>
    /// Implements <see cref="IEnumerator{T}"/> for <see cref="StoreEntry{TKey, TValue}"/>
    /// items in a segment.
    /// </summary>
    private sealed class EntryEnumerator : IEnumerator<StoreEntry<TKey, TValue>>
    {
        private readonly Driver<TKey, TValue> driver;
        private readonly ReadContext context;
        private readonly CancellationToken cancellationToken;
        private bool reachedEnd = false;
        private StoreEntry<TKey, TValue>? current = null;

        /// <summary>
        /// Initializes a new instance of EntryEnumerator.
        /// </summary>
        /// <param name="driver">
        /// The <see cref="Driver{TKey, TValue}"/> on the segment of which to
        /// enumerate the entries.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that can be used to cancel
        /// the operation.
        /// </param>
        public EntryEnumerator(
            Driver<TKey, TValue> driver,
            CancellationToken cancellationToken) :
            this(driver, 0, null, cancellationToken)
        { }

        /// <summary>
        /// Initializes a new instance of EntryEnumerator.
        /// </summary>
        /// <param name="driver">
        /// The <see cref="Driver{TKey, TValue}"/> on the segment of which to
        /// enumerate the entries.
        /// </param>
        /// <param name="start">
        /// The offset of the first entry to enumerate.
        /// </param>
        /// <param name="size">
        /// The size (in bytes) of the window starting at <paramref name="start"/>
        /// in which to enumerate entries, or null to enumerate to the end of
        /// the segment.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> value that can be used to cancel
        /// the operation.
        /// </param>
        public EntryEnumerator(
            Driver<TKey, TValue> driver,
            long start,
            long? size,
            CancellationToken cancellationToken)
        {
            Debug.Assert(driver != null, "The driver must not be null.");
            Debug.Assert(driver.reader != null, "The driver's reader must not be null.");

            this.driver = driver;
            this.context = new ReadContext(driver.reader
                .OpenDataForReadAsync(start, size, cancellationToken)
                .GetValueTaskResult());
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc/>
        public StoreEntry<TKey, TValue> Current => current!.Value;

        /// <inheritdoc/>
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public void Dispose()
        {
            context.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (reachedEnd)
            {
                return false;
            }

            // The order of fields is: flags, key, value.
            EntryFlags flags;
            try
            {
                flags = ReadEntryFlags(context);
            }
            catch (EndOfStreamException)
            {
                reachedEnd = true;
                return false;
            }

            TKey key = driver.formatter
                .ReadKeyAsync(context.Stream, cancellationToken)
                .GetValueTaskResult();
            if (!flags.HasFlag(EntryFlags.Deleted))
            {
                TValue value = driver.formatter.ReadValueAsync(context.Stream,
                                                               cancellationToken)
                                                               .GetValueTaskResult();

                current = new StoreEntry<TKey, TValue>(key, value);
            }
            else
            {
                // The flags indicate that the entry represents a deleted key,
                // so there wouldn't be a value to read.
                current = StoreEntry<TKey, TValue>.Delete(key);
            }

            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
