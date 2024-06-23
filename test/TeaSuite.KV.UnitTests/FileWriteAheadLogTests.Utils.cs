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
        BitConverter.TryWriteBytes(walSpan, 0x45_54_52_57);
        BitConverter.TryWriteBytes(walSpan[4..], key);
        byte[] rawVal = Encoding.UTF8.GetBytes(val);
        BitConverter.TryWriteBytes(walSpan[8..], rawVal.Length);
        rawVal.CopyTo(walSpan[12..]);

        return wal;
    }

    private byte[] ConstructWalWithDeleteEntry(int key)
    {
        byte[] wal = ConstructNonClosedWal();
        Span<byte> walSpan = wal.AsSpan()[(2 * (4 /* tag */ + 8 /* value */))..];

        // Delete entry
        BitConverter.TryWriteBytes(walSpan, 0x45_54_4c_44);
        BitConverter.TryWriteBytes(walSpan[4..], key);

        return wal;
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
}
