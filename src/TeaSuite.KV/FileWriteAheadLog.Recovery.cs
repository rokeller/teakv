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

    /// <inheritdoc/>
    public bool ShouldRecover()
    {
        FileInfo closed = GetWalFile(ClosedWalFileName);

        if (closed.Exists)
        {
            using Stream closedWal = closed.Open(FileMode.Open,
                                                 FileAccess.Read,
                                                 FileShare.Read);
            if (IsValidWal(closedWal))
            {
                // The 'closed' WAL file still exists and is a valid WAL, so we
                // need to recover.
                return true;
            }

            // Let's take this opportunity to delete the 'closed' WAL file, so
            // we don't bother anymore should recovery be needed.
            logger.LogWarning(
                "The WAL file '{closedWal}' is not valid, deleting.",
                closed.FullName);
            closed.Delete();
        }

        FileInfo open = GetWalFile(OpenWalFileName);
        if (!open.Exists)
        {
            return false;
        }

        using Stream openWal = open.Open(FileMode.Open,
                                         FileAccess.Read,
                                         FileShare.Read);
        if (IsValidWal(openWal))
        {
            return true;
        }
        else
        {
            // Let's take this opportunity to delete the 'open' WAL file, so we
            // don't bother anymore should recovery be started anyway.
            logger.LogWarning(
                "The WAL file '{closedWal}' is not valid, deleting.",
                open.FullName);
            open.Delete();
            return false;
        }
    }

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> Recover()
    {
        return new RecoveryEnumerator(this);
    }

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
        private State state;
        private Stream? wal;

        internal RecoveryEnumerator(FileWriteAheadLog<TKey, TValue> owner)
        {
            this.owner = owner;

            FileInfo closed = owner.GetWalFile(ClosedWalFileName);
            if (closed.Exists)
            {
                state = State.ProcessingClosedWalFile;
                wal = closed.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                return;
            }

            FileInfo open = owner.GetWalFile(OpenWalFileName);
            if (open.Exists)
            {
                state = State.ProcessingOpenWalFile;
                wal = open.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                return;
            }

            state = State.FinishedProcessing;
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
                        "Read WAL timestamp {timestamp}",
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
                    FileInfo open = owner.GetWalFile(OpenWalFileName);
                    if (open.Exists)
                    {
                        wal = open.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                        owner.logger.LogDebug(
                            "Moving to processing WAL file '{walPath}'.",
                            open.FullName);
                        state = State.ProcessingOpenWalFile;
                    }
                    else
                    {
                        state = State.FinishedProcessing;
                    }
                    return true;

                case State.ProcessingOpenWalFile:
                case State.FinishedProcessing:
                default:
                    state = State.FinishedProcessing;
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
