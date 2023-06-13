using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeaSuite.KV.Data;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

partial class DefaultKeyValueStore<TKey, TValue>
{
    /// <summary>
    /// Tracks queued factories for pending maintenance tasks.
    /// </summary>
    private readonly ConcurrentQueue<Func<Task>> maintenanceQueue = new ConcurrentQueue<Func<Task>>();

    /// <summary>
    /// Runs the maintenance loop.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> which tracks completion of all maintenance tasks.
    /// </returns>
    private async Task RunMaintenanceAsync()
    {
        TimeSpan maxWait = TimeSpan.FromSeconds(1);
        while (!storeOpen.IsCancellationRequested || maintenanceQueue.Count > 0)
        {
            if (maintenanceQueue.TryDequeue(out Func<Task>? maintenanceTaskFunc))
            {
                try
                {
                    await maintenanceTaskFunc().ConfigureAwaitLib();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected exception in maintenance task.");
                }
            }
            else
            {
                await Task.Delay(maxWait).ConfigureAwaitLib();
            }
        }
    }

    private void StartPersistMemoryStore(bool force)
    {
        void CheckAndEnqueue()
        {
            if (!force && lastPersistQueued + settings.MinimumPersistInterval > systemClock.UtcNow)
            {
                return;
            }

            Logger.LogDebug("Queueing persist. Last persisted: {lastPersisted}", lastPersistQueued);
            lastPersistQueued = systemClock.UtcNow;

            maintenanceQueue.Enqueue(() => PersistMemoryStoreAsync());
        }

        maintenanceQueue.Enqueue(() => Task.Run(CheckAndEnqueue));
    }

    private void StartMerge()
    {
        maintenanceQueue.Enqueue(MergeAsync);
    }

    private async Task PersistMemoryStoreAsync()
    {
        Debug.Assert(memoryStores.Old == null, "The current memory stores must not track a past store.");

        Stopwatch watch = Stopwatch.StartNew();
        IMemoryKeyValueStore<TKey, TValue> newStore = memoryStoreFactory.Create();
        IMemoryKeyValueStore<TKey, TValue> oldStore = memoryStores.Current;
        MemoryStores newMemoryStores = new MemoryStores(newStore, memoryStores);
        Interlocked.Exchange(ref memoryStores, newMemoryStores);
        long numEntries = 0;

        if (oldStore.Count > 0)
        {
            long newSegmentId = GetNextSegmentId();
            Logger.LogInformation("Starting to persist memory store to new segment {segmentId}.", newSegmentId);

            Segment<TKey, TValue> segment = SegmentManager.CreateNewSegment(newSegmentId);
            using Driver<TKey, TValue> driver = segment.Driver;
            using IEnumerator<StoreEntry<TKey, TValue>> enumerator = oldStore.GetOrderedEnumerator();

            // Write the entries in ascending order.
            numEntries = await driver.WriteEntriesAsync(enumerator, settings, default).ConfigureAwaitLib();

            if (!storeOpen.IsCancellationRequested)
            {
                // Add the newly created segment to the set of segments.
                Segments = Segments.Add(SegmentManager.MakeReadOnly(segment));

                // Now with the new segment added, check if a merge is needed.
                if (settings.MergePolicy.ShouldMerge(Segments.Count))
                {
                    StartMerge();
                }
            }
        }

        Interlocked.Exchange(ref memoryStores, new MemoryStores(newStore));
        watch.Stop();
        Logger.LogInformation("Finished persisting memory store with {numEntries} entries in {ms}ms.",
            numEntries, watch.ElapsedMilliseconds);
    }

    private async Task MergeAsync()
    {
        if (Segments.Count <= 1)
        {
            return;
        }

        Logger.LogDebug("Starting to merge segments ...");
        Stopwatch watch = Stopwatch.StartNew();

        // Get an ordered list of per-segment enumerators, with the youngest segment first.
        List<IEnumerator<StoreEntry<TKey, TValue>>> enumerators = Segments
            .Select(s => s.Driver.GetEntryEnumerator())
            .ToList();

        static bool IsNotDeleted(StoreEntry<TKey, TValue> entry)
        {
            return !entry.IsDeleted;
        }

        long newSegmentId = GetNextSegmentId();
        Logger.LogInformation("Merging {numSegments} into new segment {segmentId}.", enumerators.Count, newSegmentId);
        Segment<TKey, TValue> segment = SegmentManager.CreateNewSegment(newSegmentId);
        await using Driver<TKey, TValue> driver = segment.Driver;
        using MergingEnumerator<StoreEntry<TKey, TValue>> merging =
            new MergingEnumerator<StoreEntry<TKey, TValue>>(enumerators.ToArray());
        // Since we merge all currently known segments, we can skip those entries that are deleted and compress a bit.
        using FilteringEnumerator<StoreEntry<TKey, TValue>> filtered =
            new FilteringEnumerator<StoreEntry<TKey, TValue>>(merging, IsNotDeleted);

        long numEntries = await driver.WriteEntriesAsync(filtered, settings, default).ConfigureAwaitLib();
        watch.Stop();
        // TODO: Consider what to do when the resulting segment is empty because all entries were deleted and filtered.

        List<ValueTask> pendingDeletes = Segments
            .Select(s => SegmentManager.DeleteSegmentAsync(s.Id, default))
            .ToList();

        foreach (ValueTask pendingDelete in pendingDeletes)
        {
            await pendingDelete.ConfigureAwaitLib();
        }

        // Replace the set of existing segments with the newly merged segment.
        Segments = ImmutableSortedSet.Create(SegmentManager.MakeReadOnly(segment));
        Logger.LogInformation("Finished merging segments with {numEntries} entries in {ms}ms.",
            numEntries, watch.ElapsedMilliseconds);
    }
}
