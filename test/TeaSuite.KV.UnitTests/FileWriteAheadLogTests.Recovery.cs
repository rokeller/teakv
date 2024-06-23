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
    public void ShouldRecoverReturnsFalseWhenOpenWalFileIsClosed()
    {
        string closedFile = Path.Combine(walSettings.LogDirectoryPath, ".wal.open");

        using (Stream stream = File.Create(closedFile))
        {
            stream.Write(ConstructClosedWal());
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
}
