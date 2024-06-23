using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// <summary>
    /// The name of the WAL file for the currently open WAL.
    /// </summary>
    private const string OpenWalFileName = ".wal.open";
    /// <summary>
    /// The name of the WAL file for the previous WAL that has been closed but
    /// not persisted to a segment yet.
    /// </summary>
    private const string ClosedWalFileName = ".wal.closed";

    private readonly ILogger<FileWriteAheadLog<TKey, TValue>> logger;
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
    /// <param name="logger">
    /// The <see cref="ILogger"/> to use.
    /// </param>
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
        ILogger<FileWriteAheadLog<TKey, TValue>> logger,
        IFormatter<TKey> keyFormatter,
        IFormatter<TValue> valueFormatter,
        IOptionsMonitor<FileWriteAheadLogSettings> options,
        ISystemClock clock
        )
    {
        this.logger = logger;
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
