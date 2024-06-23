using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
        // If we're dealing with a valid but not closed WAL, we need to recover.
        return IsValidWal(openWal) && !IsClosedWal(openWal);
    }

    /// <inheritdoc/>
    public IEnumerator<StoreEntry<TKey, TValue>> Recover()
    {
        return new RecoveryEnumerator(this);
    }

    private enum WalEntryTag : UInt32
    {
        // Magic = 0x0000_0110,
        Magic = 0x43_49_47_4d, // 'MGIC'
        // Timestamp = 0x0001_0000,
        Timestamp = 0x45_4d_49_54, // 'TIME'
        // Close = 0x0002_0000,
        Close = 0x45_53_4c_43, // 'CLSE'
        // Write = 0x0100_0000,
        Write = 0x45_54_52_57, // 'WRTE'
        // Delete = 0x0200_0000,
        Delete = 0x45_54_4c_44, // 'DLTE'
    }

    private sealed class RecoveryEnumerator : IEnumerator<StoreEntry<TKey, TValue>>
    {
        private readonly FileWriteAheadLog<TKey, TValue> owner;

        internal RecoveryEnumerator(FileWriteAheadLog<TKey, TValue> owner)
        {
            this.owner = owner;
        }

        public StoreEntry<TKey, TValue> Current => throw new System.NotImplementedException();

        object IEnumerator.Current => throw new System.NotImplementedException();

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public bool MoveNext()
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
