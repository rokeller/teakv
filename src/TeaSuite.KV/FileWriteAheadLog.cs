using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TeaSuite.KV.IO;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV;

/// <summary>
/// Implements the <see cref="IWriteAheadLog{TKey, TValue}"/> to persist details
/// of write operations to a write-ahead log only let
/// write operations to a key-value store proceed when the operation could first
/// be written to the write-ahead log.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the key-value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the key-value store.
/// </typeparam>
public partial class FileWriteAheadLog<TKey, TValue> :
    IWriteAheadLog<TKey, TValue>, IDisposable
    where TKey : IComparable<TKey>
{
    private const int SimpleWalEntrySize = sizeof(WalEntryTag) + sizeof(long);
    /// <summary>
    /// The name of the WAL file for the currently open WAL.
    /// </summary>
    private const string OpenWalFileName = ".wal.open";
    /// <summary>
    /// The name of the WAL file for the previous WAL that has been closed but
    /// not persisted to a segment yet.
    /// </summary>
    private const string ClosedWalFileName = ".wal.closed";

    private readonly IFormatter<TKey> keyFormatter;
    private readonly IFormatter<TValue> valueFormatter;
    private readonly FileWriteAheadLogSettings settings;
    private readonly ISystemClock clock;
    private readonly DirectoryInfo walDir;
    private Stream? wal;
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of <see cref="FileWriteAheadLog{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyFormatter">
    /// The <see cref="IFormatter{T}"/> for keys.
    /// </param>
    /// <param name="valueFormatter">
    /// The <see cref="IFormatter{T}"/> for values.
    /// </param>
    /// <param name="options">
    /// An <see cref="IOptionsMonitor{TOptions}"/> of <see cref="FileWriteAheadLogSettings"/>
    /// holding settings for the write-ahead log.
    /// </param>
    /// <param name="clock">
    /// The <see cref="ISystemClock"/> to use.
    /// </param>
    public FileWriteAheadLog(
        IFormatter<TKey> keyFormatter,
        IFormatter<TValue> valueFormatter,
        IOptionsMonitor<FileWriteAheadLogSettings> options,
        ISystemClock clock
        )
    {
        this.keyFormatter = keyFormatter;
        this.valueFormatter = valueFormatter;
        settings = options.GetForStore<FileWriteAheadLogSettings, TKey, TValue>();
        this.clock = clock;

        walDir = Directory.CreateDirectory(settings.LogDirectoryPath);
    }

    /// <inheritdoc/>
    public void Start()
    {
        using GuardCompletion tx = StartGuard();
        FileInfo open = GetWalFile(OpenWalFileName);
        // Create the 'open' file and overwrite if it already exists. We would
        // have recovered from it before.
        wal = new FileStream(open.FullName,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             settings.BufferSize);
        wal.SetLength(settings.ReservedSize);
        InitWal();
    }

    /// <inheritdoc/>
    public async ValueTask<bool> AnnounceWriteAsync(StoreEntry<TKey, TValue> entry)
    {
        EnsureWalWritable();
        using GuardCompletion tx = await StartGuardAsync().ConfigureAwaitLib();
        await WriteOperationAsync(entry).ConfigureAwaitLib();
        return true;
    }

    /// <inheritdoc/>
    public async ValueTask<IDisposable> PrepareTransitionAsync()
    {
        EnsureWalWritable();
        GuardCompletion tx = await StartGuardAsync().ConfigureAwaitLib();

        // First, close up the current 'open' WAL.
        CloseWal();

        // Rename it, so we can refer back to it in case the merge fails.
        FileInfo open = GetWalFile(OpenWalFileName);
        FileInfo closed = GetWalFile(ClosedWalFileName);
        open.MoveTo(closed.FullName);

        // Now create the new 'open' WAL and initialize it.
        open = GetWalFile(OpenWalFileName);
        wal = new FileStream(open.FullName,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             settings.BufferSize);
        wal.SetLength(settings.ReservedSize);
        InitWal();

        return tx;
    }

    /// <inheritdoc/>
    public async ValueTask<IDisposable> CompleteTransitionAsync()
    {
        GuardCompletion tx = await StartGuardAsync().ConfigureAwaitLib();

        // The transition to a new WAL is finished, the old in-memory store has
        // been flushed to a segment, so we can delete the closed WAL file.
        FileInfo closed = GetWalFile(ClosedWalFileName);
        closed.Delete();

        return tx;
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        EnsureWalWritable();
        using GuardCompletion tx = StartGuard();
        CloseWal();

        // The 'open' WAL is properly closed now, so let's also try to delete
        // the file.
        FileInfo open = GetWalFile(OpenWalFileName);
        open.Delete();
    }

    /// <summary>
    /// Initializes the WAL file by writing the magic entry followed by a
    /// timestamp entry indicating the current date/time.
    /// </summary>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void InitWal()
    {
        Debug.Assert(null != wal, "The WAL must be writable.");
        WriteMagic();
        WriteTimestamp();
        wal.Flush();
    }

    /// <summary>
    /// Closes the WAL file by writing a timestamp entry indicating the current
    /// date/time, followed by a 'close' entry (indicating a clean close), plus
    /// an additional 'close' entry at the end of the file.
    /// </summary>
    /// <remarks>
    /// The caller must already have entered the operations semaphore before
    /// calling this method. The semaphore is <b>not</b> freed by this method.
    /// </remarks>
    private void CloseWal()
    {
        Debug.Assert(null != wal, "The WAL must be writable.");
        WriteTimestamp();
        WriteClose();
        wal.Seek(-SimpleWalEntrySize, SeekOrigin.End);
        WriteClose();
        wal.Close();
        wal.Dispose();
        wal = null;
    }

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

    /// <summary>
    /// Checks if the WAL in the given <paramref name="stream"/> is valid.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> representing the WAL.
    /// </param>
    /// <returns>
    /// <c>True</c> if the <paramref name="stream"/> represents a valid WAL,
    /// <c>False</c> otherwise.
    /// </returns>
    private static bool IsValidWal(Stream stream)
    {
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

    /// <summary>
    /// Checks if the WAL in the given <paramref name="stream"/> was closed
    /// properly.
    /// </summary>
    /// <param name="stream">
    /// The <see cref="Stream"/> representing the WAL.
    /// </param>
    /// <returns>
    /// <c>True</c> if the <paramref name="stream"/> represents a properly closed
    /// WAL, <c>False</c> otherwise.
    /// </returns>
    private static bool IsClosedWal(Stream stream)
    {
        stream.Seek(-SimpleWalEntrySize, SeekOrigin.End);
        (WalEntryTag tag, long value) = ReadSimpleWalEntry(stream);

        // The WAL is closed if the last entry matches the 'close' tag with the
        // 'close' value.
        return WalEntryTag.Close == tag && CloseEntryValue == value;
    }

    private static (WalEntryTag tag, long value) ReadSimpleWalEntry(Stream wal)
    {
        StreamExtensions.Read(wal, out uint tag);
        StreamExtensions.Read(wal, out long value);

        return ((WalEntryTag)tag, value);
    }

    private FileInfo GetWalFile(string name)
    {
        return new FileInfo(Path.Combine(walDir.FullName, name));
    }

    private void EnsureWalWritable()
    {
        if (null == wal)
        {
            throw new InvalidOperationException(
                "Start must be called before the first write operation.");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    /// <param name="disposing">
    /// A flag indicating whether the call is from <see cref="Dispose()"/>.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                wal?.Dispose();
            }

            disposedValue = true;
        }
    }
}
