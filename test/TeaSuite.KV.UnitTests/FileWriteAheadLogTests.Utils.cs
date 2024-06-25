using System.Text;

namespace TeaSuite.KV;

partial class FileWriteAheadLogTests
{
    private byte[] ConstructNonClosedWal()
    {
        byte[] wal = new byte[walSettings.ReservedSize];
        Span<byte> walSpan = wal;

        // Magic entry
        BitConverter.TryWriteBytes(walSpan, 0x43_49_47_4dU);
        BitConverter.TryWriteBytes(walSpan[4..], 0x00000000_2d_4c_41_57L);

        // Timestamp entry
        walSpan = walSpan[12..];
        BitConverter.TryWriteBytes(walSpan, 0x45_4d_49_54U);
        BitConverter.TryWriteBytes(walSpan[4..], utcNow.Ticks);

        return wal;
    }

    private byte[] ConstructNonClosedWalWithIncompleteEntry()
    {
        byte[] wal = ConstructNonClosedWal();
        return wal[0..20];
    }

    private byte[] ConstructNonClosedWalWithInvalidMagicVal()
    {
        byte[] wal = new byte[walSettings.ReservedSize];
        Span<byte> walSpan = wal;

        // Magic entry
        BitConverter.TryWriteBytes(walSpan, 0x43_49_47_4dU);
        BitConverter.TryWriteBytes(walSpan[4..], 1234L);

        // Timestamp entry
        walSpan = walSpan[12..];
        BitConverter.TryWriteBytes(walSpan, 0x45_4d_49_54U);
        BitConverter.TryWriteBytes(walSpan[4..], utcNow.Ticks);

        return wal;
    }

    private byte[] ConstructWalWithWriteEntry(int key, string val)
    {
        byte[] wal = ConstructNonClosedWal();
        Span<byte> walSpan = wal.AsSpan()[(2 * (4 /* tag */ + 8 /* value */))..];

        // Write entry
        AddWriteEntry(walSpan, key, val);

        return wal;
    }

    private Span<byte> AddWriteEntry(Span<byte> destination, int key, string val)
    {
        BitConverter.TryWriteBytes(destination, 0x45_54_52_57);
        BitConverter.TryWriteBytes(destination[4..], key);
        byte[] rawVal = Encoding.UTF8.GetBytes(val);
        BitConverter.TryWriteBytes(destination[8..], rawVal.Length);
        rawVal.CopyTo(destination[12..]);

        return destination[(12 + rawVal.Length)..];
    }

    private byte[] ConstructWalWithDeleteEntry(int key)
    {
        byte[] wal = ConstructNonClosedWal();
        Span<byte> walSpan = wal.AsSpan()[(2 * (4 /* tag */ + 8 /* value */))..];

        // Delete entry
        AddDeleteEntry(walSpan, key);

        return wal;
    }

    private Span<byte> AddDeleteEntry(Span<byte> destination, int key)
    {
        BitConverter.TryWriteBytes(destination, 0x45_54_4c_44);
        BitConverter.TryWriteBytes(destination[4..], key);

        return destination[8..];
    }

    private byte[] ConstructClosedWal()
    {
        byte[] wal = ConstructNonClosedWal();
        Span<byte> walSpan = wal.AsSpan()[(2 * (4 /* tag */ + 8 /* value */))..];

        // Timestamp entry
        BitConverter.TryWriteBytes(walSpan, 0x45_4d_49_54U);
        BitConverter.TryWriteBytes(walSpan[4..], utcNow.Ticks);

        // Close entry
        walSpan = walSpan[12..];
        BitConverter.TryWriteBytes(walSpan, 0x45_53_4c_43);
        BitConverter.TryWriteBytes(walSpan[4..], 0L);

        // Before-EOF Close Entry
        walSpan = walSpan[^12..];
        BitConverter.TryWriteBytes(walSpan, 0x45_53_4c_43);
        BitConverter.TryWriteBytes(walSpan[4..], 0L);

        return wal;
    }

    private byte[] ConstructWalWithEntries(IEnumerable<StoreEntry<int, string>> entries)
    {
        (byte[] wal, _) = ConstructWalWithEntriesForReuse(entries);

        return wal;
    }

    private (byte[], int offset) ConstructWalWithEntriesForReuse(IEnumerable<StoreEntry<int, string>> entries)
    {
        byte[] wal = ConstructNonClosedWal();
        Span<byte> walSpan = wal.AsSpan()[(2 * (4 /* tag */ + 8 /* value */))..];

        foreach (StoreEntry<int, string> entry in entries)
        {
            if (entry.IsDeleted)
            {
                walSpan = AddDeleteEntry(walSpan, entry.Key);
            }
            else
            {
                walSpan = AddWriteEntry(walSpan, entry.Key, entry.Value!);
            }
        }

        return (wal, wal.Length - walSpan.Length);
    }

    private byte[] ConstructClosedWalWithEntries(IEnumerable<StoreEntry<int, string>> entries)
    {
        (byte[] wal, int offset) = ConstructWalWithEntriesForReuse(entries);
        Span<byte> walSpan = wal.AsSpan()[offset..];
        BitConverter.TryWriteBytes(walSpan, 0x45_53_4c_43);
        BitConverter.TryWriteBytes(walSpan[4..], 0L);

        return wal;
    }
}
