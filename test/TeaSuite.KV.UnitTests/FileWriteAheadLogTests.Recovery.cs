using System.Collections;
using AutoFixture.Xunit2;

namespace TeaSuite.KV;

partial class FileWriteAheadLogTests
{
    [Fact]
    public void ShouldRecoverReturnsFalseWhenWalFilesMissing()
    {
        Assert.False(wal.ShouldRecover());
    }

    [Fact]
    public void ShouldRecoverReturnsFalseWhenWalFilesExistButAreEmpty()
    {
        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");

        using (File.Create(openFile)) { }
        using (File.Create(closedFile)) { }

        Assert.False(wal.ShouldRecover());
    }

    [Fact]
    public void ShouldRecoverReturnsFalseWhenClosedWalFileIsInvalid()
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");

        using (Stream stream = File.Create(closedFile))
        {
            stream.SetLength(128);
        }

        Assert.False(wal.ShouldRecover());

        // Create a WAL with an invalid magic value.
        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructNonClosedWalWithInvalidMagicVal());
        }

        Assert.False(wal.ShouldRecover());
    }

    [Fact]
    public void ShouldRecoverReturnsFalseWhenOpenWalFileIsInvalid()
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");

        using (Stream stream = File.Create(closedFile))
        {
            stream.SetLength(128);
        }

        Assert.False(wal.ShouldRecover());

        // Create a WAL with an invalid magic value.
        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructNonClosedWalWithInvalidMagicVal());
        }

        Assert.False(wal.ShouldRecover());
    }

    [Fact]
    public void ShouldRecoverReturnsTrueWhenClosedWalIsValid()
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");

        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructNonClosedWal());
        }

        Assert.True(wal.ShouldRecover());
    }

    [Fact]
    public void ShouldRecoverReturnsTrueWhenOpenWalIsNotClosed()
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");

        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructNonClosedWal());
        }

        Assert.True(wal.ShouldRecover());
    }

    [Theory, AutoData]
    public void RecoverWorksForClosedWal(int key, string val)
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructWalWithWriteEntry(key, val));
        }

        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new StoreEntry<int, string>(key, val), enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Theory, AutoData]
    public void RecoverWorksForOpenWal(int key)
    {
        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (Stream stream = File.Create(openFile))
        {
            stream.Write(ConstructWalWithDeleteEntry(key));
        }

        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(StoreEntry<int, string>.Delete(key), enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void RecoverYieldsNothingForMissingWalFiles()
    {
        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        Assert.False(enumerator.MoveNext());
    }

    [Theory, AutoData]
    public void RecoverWorksForCombinationOfClosedAndOpenWal(
        Generator<int> keyGen,
        Generator<string> valGen)
    {
        walSettings.ReservedSize = 1024;
        int nextKey = 0, nextVal = 0;
        List<int> keys = keyGen.Take(6).ToList();
        List<string> values = valGen.Take(3).ToList();
        List<StoreEntry<int, string>> expectedEntries = new List<StoreEntry<int, string>>();

        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (Stream stream = File.Create(closedFile))
        {
            List<StoreEntry<int, string>> entries = new List<StoreEntry<int, string>>()
            {
                new StoreEntry<int, string>(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new StoreEntry<int, string>(keys[nextKey++], values[nextVal++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructClosedWalWithEntries(entries));
        }

        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (Stream stream = File.Create(openFile))
        {
            List<StoreEntry<int, string>> entries = new List<StoreEntry<int, string>>()
            {
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new StoreEntry<int, string>(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructClosedWalWithEntries(entries));
        }

        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        foreach (StoreEntry<int, string> expected in expectedEntries)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(expected, enumerator.Current);
            Assert.Equal(expected, ((IEnumerator)enumerator).Current);
        }

        Assert.False(enumerator.MoveNext());
    }

    [Theory, AutoData]
    public void RecoverWorksForEmptyClosedAndGoodOpenWal(int key, string val)
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (Stream stream = File.Create(closedFile))
        { }

        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (Stream stream = File.Create(openFile))
        {
            stream.Write(ConstructWalWithWriteEntry(key, val));
        }

        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new StoreEntry<int, string>(key, val), enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void RecoverResetThrows()
    {
        using IEnumerator<StoreEntry<int, string>> enumerator = wal.Recover();
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
    }
}
