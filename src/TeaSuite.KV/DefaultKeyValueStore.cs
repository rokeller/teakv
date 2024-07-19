using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeaSuite.KV.Data;
using TeaSuite.KV.IO;
using TeaSuite.KV.Policies;

namespace TeaSuite.KV;

/// <summary>
/// Default implementation of the Key/Value store. Uses the currently configured
/// <see cref="IMemoryKeyValueStoreFactory{TKey, TValue}"/> to create in-memory
/// stores for write operations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public partial class DefaultKeyValueStore<TKey, TValue>
    : BaseKeyValueStore<TKey, TValue>, IAsyncDisposable, IDisposable
    where TKey : IComparable<TKey>
{
    private readonly ILogger<DefaultKeyValueStore<TKey, TValue>> logger;
    private readonly IWriteAheadLog<TKey, TValue> wal;
    private readonly IMemoryKeyValueStoreFactory<TKey, TValue> memoryStoreFactory;
    private readonly ILockingPolicy lockingPolicy;
    private readonly StoreSettings settings;
    private readonly ISystemClock systemClock;
    private readonly Task pendingMaintenance;
    private readonly CancellationTokenSource storeOpen = new CancellationTokenSource();
    private bool isDisposed;

    /// <summary>
    /// Holds references to the current in-memory store and the past one, iff a
    /// flush/persist is in progress.
    /// </summary>
    private volatile MemoryStores memoryStores;

    /// <summary>
    /// The timestamp of the last flush/persist operation for the in-memory store.
    /// </summary>
    private DateTimeOffset lastPersistQueued;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    /// <param name="logger">
    /// The <see cref="ILogger{TCategoryName}"/> to use.
    /// </param>
    /// <param name="wal">
    /// The <see cref="IWriteAheadLog{TKey, TValue}"/> to use to log write
    /// operations to the in-memory store for recovery in case of a crash.
    /// </param>
    /// <param name="memoryStoreFactory">
    /// The <see cref="IMemoryKeyValueStoreFactory{TKey, TValue}"/> to use to
    /// create new instances of <see cref="IMemoryKeyValueStore{TKey, TValue}"/>.
    /// </param>
    /// <param name="lockingPolicy">
    /// The <see cref="ILockingPolicy"/> to use when reading and writing to the
    /// in-memory key-value store.
    /// </param>
    /// <param name="segmentManager">
    /// The <see cref="ISegmentManager{TKey, TValue}"/> to use to manage persisted
    /// segments of the store.
    /// </param>
    /// <param name="options">
    /// An <see cref="IOptionsMonitor{TOptions}"/> of
    /// <see cref="StoreOptions{TKey, TValue}"/> holding settings for the store.
    /// </param>
    /// <param name="systemClock">
    /// The <see cref="ISystemClock"/> instance to use.
    /// </param>
    public DefaultKeyValueStore(
        ILogger<DefaultKeyValueStore<TKey, TValue>> logger,
        IWriteAheadLog<TKey, TValue> wal,
        IMemoryKeyValueStoreFactory<TKey, TValue> memoryStoreFactory,
        ILockingPolicy lockingPolicy,
        ISegmentManager<TKey, TValue> segmentManager,
        IOptionsMonitor<StoreOptions<TKey, TValue>> options,
        ISystemClock systemClock)
        : base(logger, segmentManager)
    {
        this.logger = logger;
        this.wal = wal;
        this.memoryStoreFactory = memoryStoreFactory;
        this.lockingPolicy = lockingPolicy;
        this.settings = options.CurrentValue.Settings;
        this.systemClock = systemClock;

        // Start the maintenance worker.
        pendingMaintenance = RunMaintenanceAsync();
        memoryStores = new MemoryStores(memoryStoreFactory.Create());
        wal.Start(Recover);
        lastPersistQueued = systemClock.UtcNow;
    }

    /// <inheritdoc/>
    public override bool TryGet(TKey key, out TValue? value)
    {
        StoreEntry<TKey, TValue> entry;

        using (lockingPolicy.AcquireReadLock())
        {
            IMemoryKeyValueStore<TKey, TValue>? oldStore = memoryStores.Old;
            if (memoryStores.Current.TryGet(key, out entry))
            {
                goto EntryFound;
            }
            // If we're in the middle of flushing the memory store, check the
            // old memory store too, if available.
            else if (null != oldStore && oldStore.TryGet(key, out entry))
            {
                goto EntryFound;
            }
        }
        // We couldn't find the entry in the in-memory stores. It's time
        // to check the segments.
        if (!TryGetFromSegments(key, out entry))
        {
            entry = StoreEntry<TKey, TValue>.Delete(key);
        }

    EntryFound:
        if (entry.IsDeleted)
        {
            value = default;
            return false;
        }
        else
        {
            value = entry.Value;
            return true;
        }
    }

    /// <inheritdoc/>
    public override void Set(TKey key, TValue value)
    {
        WriteEntry(new StoreEntry<TKey, TValue>(key, value));
    }

    /// <inheritdoc/>
    public override void Delete(TKey key)
    {
        WriteEntry(StoreEntry<TKey, TValue>.Delete(key));
    }

    /// <inheritdoc/>
    public override void Close()
    {
        // If we do have any write operations that haven't been persisted yet,
        // now's the time.
        if (memoryStores.Current.Count > 0)
        {
            StartPersistMemoryStore(force: true);
        }

        storeOpen.Cancel();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwaitLib();

        Dispose(disposing: false);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Starts a merge operation, provided the current <see cref="IMergePolicy"/>
    /// allows it.
    /// </summary>
    public void Merge()
    {
        if (settings.MergePolicy.ShouldMerge(Segments.Count))
        {
            StartMerge();
        }
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    /// <param name="disposing">
    /// A flag which indicates whether the call originated from the
    /// <see cref="Dispose()"/> method.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                Close();

                Logger.LogDebug("Waiting for maintenance to finish...");
                pendingMaintenance.GetTaskResult();
                Logger.LogInformation("Finished maintenance.");
                ShutdownWal();
                Logger.LogInformation("Write-ahead log shutdown.");

                storeOpen.Dispose();
            }

            isDisposed = true;
        }
    }

    /// <summary>
    /// Asynchronously disposes this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        Close();

        Logger.LogDebug("Waiting for maintenance to finish...");
        await pendingMaintenance.ConfigureAwaitLib();
        Logger.LogInformation("Finished maintenance.");
        ShutdownWal();
        Logger.LogInformation("Write-ahead log shutdown.");

        storeOpen.Dispose();
    }

    /// <summary>
    /// Writes the entry unless the write operation cannot be completed.
    /// </summary>
    /// <param name="entry">
    /// The <see cref="StoreEntry{TKey, TValue}"/> to write to the store.
    /// </param>
    private void WriteEntry(StoreEntry<TKey, TValue> entry)
    {
        using (lockingPolicy.AcquireWriteLock())
        {
            if (wal.AnnounceWriteAsync(entry).GetValueTaskResult())
            {
                memoryStores.Current.Set(entry);
                QueuePersistIfNeeded();
            }
        }
    }

    /// <summary>
    /// Checks if a persist on the in-memory store is needed/desired.
    /// </summary>
    private void QueuePersistIfNeeded()
    {
        if (settings.PersistPolicy.ShouldPersist(memoryStores.Current.Count,
                                                 systemClock.UtcNow - lastPersistQueued))
        {
            StartPersistMemoryStore(force: false);
        }
    }

    /// <summary>
    /// Recovers from an earlier crash by re-applying the write operations
    /// (<see cref="IKeyValueStore{TKey, TValue}.Set(TKey, TValue)"/> and
    /// <see cref="IKeyValueStore{TKey, TValue}.Delete(TKey)"/>) from
    /// the write-ahead log.
    /// </summary>
    /// <param name="enumerator">
    /// The <see cref="IEnumerator{T}"/> of <see cref="StoreEntry{TKey, TValue}"/>
    /// values that represent the write operations to recover.
    /// </param>
    private void Recover(IEnumerator<StoreEntry<TKey, TValue>> enumerator)
    {
        using (enumerator)
        {
            while (enumerator.MoveNext())
            {
                WriteEntry(enumerator.Current);
            }
        }
    }

    private void ShutdownWal()
    {
        wal.Shutdown();
        using (wal.CompleteTransitionAsync().GetValueTaskResult())
        {
            // Intentionally left blank.
        };
    }
}
