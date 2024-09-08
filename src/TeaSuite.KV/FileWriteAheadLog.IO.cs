using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

partial class FileWriteAheadLog<TKey, TValue>
{
    private const int SimpleWalEntrySize = sizeof(WalEntryTag) + sizeof(long);

    /// <summary>
    /// Writes the magic header entry to the WAL.
    /// </summary>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void WriteMagic()
    {
        WriteSimpleWalEntry(WalEntryTag.Magic, MagicEntryValue);
    }

    /// <summary>
    /// Writes a timestamp entry to the WAL.
    /// </summary>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void WriteTimestamp()
    {
        WriteSimpleWalEntry(WalEntryTag.Timestamp, clock.UtcNow.Ticks);
    }

    /// <summary>
    /// Writes the 'close' entry to the WAL, indicating that the WAL has been
    /// completed.
    /// </summary>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void WriteClose()
    {
        WriteSimpleWalEntry(WalEntryTag.Close, CloseEntryValue);
    }

    /// <summary>
    /// Writes a simple entry to the WAL.
    /// </summary>
    /// <param name="tag">
    /// The <see cref="WalEntryTag"/> to write.
    /// </param>
    /// <param name="val">
    /// The value to write for the entry.
    /// </param>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void WriteSimpleWalEntry(WalEntryTag tag, long val)
    {
        Debug.Assert(null != wal, "The WAL must be writable.");

        StreamExtensions.Write(wal, (uint)tag);
        StreamExtensions.Write(wal, val);
    }

    /// <summary>
    /// Writes the operation for the given <paramref name="entry"/> to the WAL.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> operation to write.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that completes when the operation completes.
    /// </returns>
    private async ValueTask WriteOperationAsync(StoreEntry<TKey, TValue> entry)
    {
        Debug.Assert(null != wal, "The WAL must be writable.");
        WalEntryTag tag = entry.IsDeleted ? WalEntryTag.Delete : WalEntryTag.Write;
        StreamExtensions.Write(wal, (uint)tag);

        await keyFormatter.WriteAsync(entry.Key, wal, default).ConfigureAwaitLib();
        if (!entry.IsDeleted)
        {
            await valueFormatter.WriteAsync(entry.Value!, wal, default).ConfigureAwaitLib(); ;
        }

        await wal.FlushAsync().ConfigureAwaitLib();
    }

    private bool TryReadWalEntry(Stream wal, out WalEntry entry)
    {
        try
        {
            StreamExtensions.Read(wal, out uint rawTag);
            WalEntryTag tag = (WalEntryTag)rawTag;

            switch (tag)
            {
                case WalEntryTag.Magic:
                case WalEntryTag.Timestamp:
                case WalEntryTag.Close:
                    StreamExtensions.Read(wal, out long value);
                    entry = new(tag, value, null);
                    return true;

                case WalEntryTag.Write:
                case WalEntryTag.Delete:
                    TKey key = keyFormatter.ReadAsync(wal, default).GetValueTaskResult();
                    StoreEntry<TKey, TValue> storeEntry;
                    if (tag == WalEntryTag.Delete)
                    {
                        storeEntry = StoreEntry<TKey, TValue>.Delete(key);
                    }
                    else
                    {
                        TValue val = valueFormatter.ReadAsync(wal, default).GetValueTaskResult();
                        storeEntry = new(key, val);
                    }
                    entry = new(tag, null, storeEntry);
                    return true;

                default:
                    entry = default;
                    return false;
            }
        }
        catch (EndOfStreamException)
        {
            entry = default;
            return false;
        }
    }

    private static (WalEntryTag tag, long value) ReadSimpleWalEntry(Stream wal)
    {
        StreamExtensions.Read(wal, out uint tag);
        StreamExtensions.Read(wal, out long value);

        return ((WalEntryTag)tag, value);
    }

    /// <summary>
    /// Checks if the WAL in the given <paramref name="walFile"/> is valid.
    /// </summary>
    /// <param name="walFile">
    /// The <see cref="FileInfo"/> representing the WAL.
    /// </param>
    /// <returns>
    /// <c>True</c> if the <paramref name="walFile"/> represents a valid WAL,
    /// <c>False</c> otherwise.
    /// </returns>
    private static bool IsValidWal(FileInfo walFile)
    {
        using Stream stream = walFile.Open(FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.Read);
        try
        {
            (WalEntryTag tag, long value) = ReadSimpleWalEntry(stream);
            // The WAL is valid if the first entry matches the 'magic' tag with
            // the 'magic' value.
            return WalEntryTag.Magic == tag && MagicEntryValue == value;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    private readonly record struct WalEntry(
        WalEntryTag Tag,
        long? SimpleValue,
        StoreEntry<TKey, TValue>? StoreEntry);
}
