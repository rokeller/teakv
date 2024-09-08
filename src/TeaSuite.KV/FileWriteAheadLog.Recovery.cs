using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TeaSuite.KV;

partial class FileWriteAheadLog<TKey, TValue>
{
    // Magic value: "WAL-\0\0\0\0" in little endian
    private const long MagicEntryValue = 0x00000000_2d_4c_41_57;
    private const long CloseEntryValue = 0;

    private const string WalUnderRecoverySuffix = ".recover";

    private Recovery PrepareRecovery()
    {
        Recovery recovery = new();
        FileInfo closed = GetWalFile(ClosedWalFileName);

        if (closed.Exists)
        {
            if (IsValidWal(closed))
            {
                // The 'closed' WAL file still exists and is a valid WAL, so we
                // need to recover.
                recovery = recovery.AddClosedWal(closed);
            }
            else
            {
                // Let's take this opportunity to delete the 'closed' WAL file
                // so it won't cause any issues down the road.
                logger.LogWarning(
                    "The WAL file '{closedWal}' is not valid, deleting.",
                    closed.FullName);
                closed.Delete();
            }
        }

        FileInfo open = GetWalFile(OpenWalFileName);
        if (open.Exists)
        {
            if (IsValidWal(open))
            {
                // The 'closed' WAL file still exists and is a valid WAL, so we
                // need to recover.
                recovery = recovery.AddOpenWal(open);
            }
            else
            {
                // Let's take this opportunity to delete the 'open' WAL file
                // so it won't cause any issues down the road.
                logger.LogWarning(
                    "The WAL file '{openWal}' is not valid, deleting.",
                    closed.FullName);
                closed.Delete();
            }
        }

        return recovery;
    }

    /// <summary>
    /// Tracks the WAL files that must be included in recovery.
    /// </summary>
    private readonly record struct Recovery(
        FileInfo? ClosedWal,
        FileInfo? OpenWal)
    {
        internal bool NeedsRecovery => null != ClosedWal || null != OpenWal;

        internal Recovery AddClosedWal(FileInfo closedWal)
        {
            // Rename the file so as to not cause issues with normal operation
            // of the WAL.
            return new(MoveWalFile(closedWal), OpenWal);
        }

        internal Recovery AddOpenWal(FileInfo openWal)
        {
            // Rename the file so as to not cause issues with normal operation
            // of the WAL.
            return new(ClosedWal, MoveWalFile(openWal));
        }

        internal FileStream OpenClosedWal()
        {
            Debug.Assert(null != ClosedWal, "The 'closed' WAL must not be null.");
            return OpenWalFile(ClosedWal);
        }

        internal FileStream OpenOpenWal()
        {
            Debug.Assert(null != OpenWal, "The 'open' WAL must not be null.");
            return OpenWalFile(OpenWal);
        }

        internal void Cleanup()
        {
            if (null != ClosedWal)
            {
                ClosedWal.Delete();
            }

            if (null != OpenWal)
            {
                OpenWal.Delete();
            }
        }

        private static FileInfo MoveWalFile(FileInfo walFile)
        {
            FileInfo target = new(walFile.FullName + WalUnderRecoverySuffix);
            if (target.Exists)
            {
                target.Delete();
            }

            walFile.MoveTo(target.FullName);
            target.Refresh();
            return target;
        }

        private static FileStream OpenWalFile(FileInfo walFile)
        {
            return walFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    };

    private enum WalEntryTag : UInt32
    {
        Magic = 0x43_49_47_4d, // 'MGIC'
        Timestamp = 0x45_4d_49_54, // 'TIME'
        Close = 0x45_53_4c_43, // 'CLSE'
        Write = 0x45_54_52_57, // 'WRTE'
        Delete = 0x45_54_4c_44, // 'DLTE'
    }

    private sealed class RecoveryEnumerator : IEnumerator<StoreEntry<TKey, TValue>>
    {
        private readonly FileWriteAheadLog<TKey, TValue> owner;
        private readonly Recovery recovery;
        private State state;
        private Stream? wal;

        internal RecoveryEnumerator(
            FileWriteAheadLog<TKey, TValue> owner,
            Recovery recovery)
        {
            this.owner = owner;
            this.recovery = recovery;

            if (null != recovery.ClosedWal)
            {
                state = State.ProcessingClosedWalFile;
                wal = recovery.OpenClosedWal();
                owner.logger.LogDebug(
                    "Starting WAL recovery with '{walPath}'.",
                    recovery.ClosedWal.FullName);

                return;
            }

            Debug.Assert(null != recovery.OpenWal,
                "We must not start recovery if neither the 'closed' or the 'open' WAL file exists.");
            state = State.ProcessingOpenWalFile;
            wal = recovery.OpenOpenWal();
            owner.logger.LogDebug(
                "Starting WAL recovery with '{walPath}'.",
                recovery.OpenWal.FullName);
        }

        public StoreEntry<TKey, TValue> Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            wal?.Dispose();
        }

        public bool MoveNext()
        {
            switch (state)
            {
                case State.ProcessingClosedWalFile:
                case State.ProcessingOpenWalFile:
                    return ReadNextWalEntry();

                case State.FinishedProcessing:
                default:
                    return false;
            }
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }

        private bool ReadNextWalEntry()
        {
            Debug.Assert(null != wal, "The WAL stream must not be null.");

            while (true)
            {
                if (!owner.TryReadWalEntry(wal, out WalEntry entry))
                {
                    if (!MoveToNextState())
                    {
                        return false;
                    }

                    continue;
                }

                bool? res = HandleWalEntry(entry);
                if (res.HasValue)
                {
                    return res.Value;
                }
            }
        }

        private bool? HandleWalEntry(WalEntry entry)
        {
            switch (entry.Tag)
            {
                case WalEntryTag.Magic:
                    break;

                case WalEntryTag.Timestamp:
                    Debug.Assert(entry.SimpleValue.HasValue, "The simple value must be set.");
                    owner.logger.LogDebug(
                        "Read WAL timestamp {timestamp:O}",
                        new DateTime(entry.SimpleValue!.Value, DateTimeKind.Utc));
                    break;

                case WalEntryTag.Close:
                    // This WAL is closed, see if there's another one ...
                    if (!MoveToNextState())
                    {
                        return false;
                    }
                    // ... and try to read its next entry.
                    return ReadNextWalEntry();

                case WalEntryTag.Write:
                case WalEntryTag.Delete:
                    Debug.Assert(entry.StoreEntry.HasValue, "The store entry must be set.");
                    Current = entry.StoreEntry.Value;
                    return true;
            }

            return null;
        }

        private bool MoveToNextState()
        {
            switch (state)
            {
                case State.ProcessingClosedWalFile:
                    if (null != recovery.OpenWal)
                    {
                        wal = recovery.OpenOpenWal();
                        owner.logger.LogDebug(
                            "Moving to processing WAL file '{walPath}'.",
                            recovery.OpenWal.FullName);
                        state = State.ProcessingOpenWalFile;
                        return true;
                    }
                    goto case State.FinishedProcessing;

                case State.ProcessingOpenWalFile:
                case State.FinishedProcessing:
                default:
                    wal = null;
                    state = State.FinishedProcessing;
                    recovery.Cleanup();
                    return false;
            }
        }

        private enum State
        {
            ProcessingClosedWalFile,
            ProcessingOpenWalFile,
            FinishedProcessing,
        }
    }
}
