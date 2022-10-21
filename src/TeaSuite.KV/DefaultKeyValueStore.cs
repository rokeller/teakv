using System;
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
/// <see cref="IMemoryKeyValueStoreFactory{TKey, TValue}"/> to create in-memory stores for write operations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used for entries in the Key/Value store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used for entries in the Key/Value store.
/// </typeparam>
public partial class DefaultKeyValueStore<TKey, TValue> : BaseKeyValueStore<TKey, TValue>, IAsyncDisposable, IDisposable
    where TKey : IComparable<TKey>
{
    private readonly ILogger<DefaultKeyValueStore<TKey, TValue>> logger;
    private readonly IMemoryKeyValueStoreFactory<TKey, TValue> memoryStoreFactory;
    private readonly ISegmentManager<TKey, TValue> segmentManager;
    private readonly IOptionsSnapshot<StoreOptions<TKey, TValue>> options;
    private readonly StoreSettings settings;
    private readonly ISystemClock systemClock;
    private readonly Task pendingMaintenance;
    private readonly CancellationTokenSource storeOpen = new CancellationTokenSource();
    private bool isDisposed;

    /// <summary>
    /// Holds references to the current in-memory store and the past one, iff a flush/persist is in progress.
    /// </summary>
    private volatile MemoryStores memoryStores;

    /// <summary>
    /// The timestamp of the last flush/persist operation for the in-memory store.
    /// </summary>
    private DateTimeOffset lastPersistQueued;

    public DefaultKeyValueStore(
        ILogger<DefaultKeyValueStore<TKey, TValue>> logger,
        IMemoryKeyValueStoreFactory<TKey, TValue> memoryStoreFactory,
        ISegmentManager<TKey, TValue> segmentManager,
        IOptionsSnapshot<StoreOptions<TKey, TValue>> options,
        ISystemClock systemClock)
        : base(logger, segmentManager)
    {
        this.logger = logger;
        this.memoryStoreFactory = memoryStoreFactory;
        this.segmentManager = segmentManager;
        this.options = options;
        this.settings = options.Value.Settings;
        this.systemClock = systemClock;

        // Start the maintenance worker.
        pendingMaintenance = RunMaintenanceAsync();
        memoryStores = new MemoryStores(memoryStoreFactory.Create());

        lastPersistQueued = systemClock.UtcNow;
    }

    /// <inheritdoc/>
    public override bool TryGet(TKey key, out TValue? value)
    {
        StoreEntry<TKey, TValue> entry;

        IMemoryKeyValueStore<TKey, TValue>? oldStore = memoryStores.Old;
        if (!memoryStores.Current.TryGet(key, out entry))
        {
            // If we're in the middle of flushing the memory store, check the old memory store too, if available.
            if (null == oldStore || !oldStore.TryGet(key, out entry))
            {
                // We couldn't find the entry in the in-memory stores. It's time to check the segments.
                if (!TryGetFromSegments(key, out entry))
                {
                    value = default;
                    return false;
                }
            }
        }

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
        memoryStores.Current.Set(new StoreEntry<TKey, TValue>(key, value));
        QueuePersistIfNeeded();
    }

    /// <inheritdoc/>
    public override void Delete(TKey key)
    {
        memoryStores.Current.Set(StoreEntry<TKey, TValue>.Delete(key));
        QueuePersistIfNeeded();
    }

    /// <inheritdoc/>
    public override void Close()
    {
        // If we do have any write operations that haven't been persisted yet, now's the time.
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
    /// Starts a merge operation, provided the current <see cref="IMergePolicy"/> allows it.
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
    /// A flag which indicates whether the call originated from the <see cref="Dispose"/> method.
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

        storeOpen.Dispose();
    }

    /// <summary>
    /// Checks if a persist on the in-memory store is needed/desired.
    /// </summary>
    private void QueuePersistIfNeeded()
    {
        if (settings.PersistPolicy.ShouldPersist(memoryStores.Current.Count, systemClock.UtcNow - lastPersistQueued))
        {
            StartPersistMemoryStore(force: false);
        }
    }
}
