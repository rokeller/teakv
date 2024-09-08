using System.Collections;
using AutoFixture.Xunit2;

namespace TeaSuite.KV;

partial class FileWriteAheadLogTests
{
    [Fact]
    public void RecoverySkippedWhenWalFilesMissing()
    {
        int numInvocations = 0;
        wal.Start((_) => numInvocations++);
        Assert.Equal(0, numInvocations);
    }

    [Fact]
    public void RecoverySkippedWhenWalFilesExistButAreEmpty()
    {
        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");

        using (File.Create(openFile)) { }
        using (File.Create(closedFile)) { }

        int numInvocations = 0;
        wal.Start((_) => numInvocations++);
        Assert.Equal(0, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void RecoverySkippedWhenWalFileIsInvalid_OnlyZeroes(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.SetLength(128);
        }

        int numInvocations = 0;
        wal.Start((_) => numInvocations++);
        Assert.Equal(0, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void RecoverySkippedWhenWalFileIsInvalid_InvalidMagic(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructNonClosedWalWithInvalidMagicVal());
        }

        int numInvocations = 0;
        wal.Start((_) => numInvocations++);
        Assert.Equal(0, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void RecoveryStartedWhenWalIsValid(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructNonClosedWal());
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                // The WAL is empty, there must not be entries.
                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void RecoveryStartedWhenWalIsValidButWithIncompleteEntries(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructNonClosedWalWithIncompleteEntry());
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                // The WAL is empty, there must not be entries.
                Assert.False(e.MoveNext());
                // There must still not be any more entries.
                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void RecoveryStartedWhenRecoveryWalAlreadyExists(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        File.Create(walFile + ".recover");
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructNonClosedWal());
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                // The WAL is empty, there must not be entries.
                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory]
    [InlineAutoData(".wal.closed")]
    [InlineAutoData(".wal.open")]
    public void RecoverWorksForIncompleteEnumeration(string walFileName, int key, string val)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructWalWithWriteEntry(key, val));
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                // The WAL has one entry.
                Assert.True(e.MoveNext());
                Assert.Equal(new(key, val), e.Current);
                // We don't check if there are more entries.
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory]
    [InlineAutoData(".wal.closed")]
    [InlineAutoData(".wal.open")]
    public void RecoverWorksForSingleWalWithSingleEntry(string walFileName, int key, string val)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructWalWithWriteEntry(key, val));
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                // The WAL has one entry.
                Assert.True(e.MoveNext());
                Assert.Equal(new(key, val), e.Current);
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory, AutoData]
    public void RecoverWorksForCombinationOfClosedWals(
        Generator<int> keyGen,
        Generator<string> valGen)
    {
        walSettings.ReservedSize = 1024;
        int nextKey = 0, nextVal = 0;
        List<int> keys = keyGen.Take(6).ToList();
        List<string> values = valGen.Take(3).ToList();
        List<StoreEntry<int, string>> expectedEntries = new();

        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (Stream stream = File.Create(closedFile))
        {
            List<StoreEntry<int, string>> entries = new()
            {
                new(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new(keys[nextKey++], values[nextVal++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructClosedWalWithEntries(entries));
        }

        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (Stream stream = File.Create(openFile))
        {
            List<StoreEntry<int, string>> entries = new()
            {
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructClosedWalWithEntries(entries));
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                foreach (StoreEntry<int, string> expected in expectedEntries)
                {
                    Assert.True(e.MoveNext());
                    Assert.Equal(expected, e.Current);
                    Assert.Equal(expected, ((IEnumerator)e).Current);
                }

                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory, AutoData]
    public void RecoverWorksForCombinationOfNonClosedWals(
        Generator<int> keyGen,
        Generator<string> valGen)
    {
        walSettings.ReservedSize = 1024;
        int nextKey = 0, nextVal = 0;
        List<int> keys = keyGen.Take(6).ToList();
        List<string> values = valGen.Take(3).ToList();
        List<StoreEntry<int, string>> expectedEntries = new();

        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.closed");
        using (Stream stream = File.Create(closedFile))
        {
            List<StoreEntry<int, string>> entries = new()
            {
                new(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new(keys[nextKey++], values[nextVal++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructWalWithEntries(entries));
        }

        string openFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");
        using (Stream stream = File.Create(openFile))
        {
            List<StoreEntry<int, string>> entries = new()
            {
                StoreEntry<int, string>.Delete(keys[nextKey++]),
                new(keys[nextKey++], values[nextVal++]),
                StoreEntry<int, string>.Delete(keys[nextKey++]),
            };
            expectedEntries.AddRange(entries);
            stream.Write(ConstructWalWithEntries(entries));
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                foreach (StoreEntry<int, string> expected in expectedEntries)
                {
                    Assert.True(e.MoveNext());
                    Assert.Equal(expected, e.Current);
                    Assert.Equal(expected, ((IEnumerator)e).Current);
                }

                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
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

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.NotNull(e);
            using (e)
            {
                Assert.True(e.MoveNext());
                Assert.Equal(new(key, val), e.Current);
                Assert.False(e.MoveNext());
            }
        });
        Assert.Equal(1, numInvocations);
    }

    [Theory]
    [InlineData(".wal.closed")]
    [InlineData(".wal.open")]
    public void ResetOnRecoveryEnumeratorThrows(string walFileName)
    {
        string walFile = Path.Combine(walSettings.LogDirectoryPath, walFileName);
        using (Stream stream = File.Create(walFile))
        {
            stream.Write(ConstructNonClosedWal());
        }

        int numInvocations = 0;
        wal.Start((e) =>
        {
            numInvocations++;
            Assert.Throws<InvalidOperationException>(() => e.Reset());
        });
        Assert.Equal(1, numInvocations);
    }
}
