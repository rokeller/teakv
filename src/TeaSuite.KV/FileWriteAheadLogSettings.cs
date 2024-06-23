namespace TeaSuite.KV;

/// <summary>
/// Defines settings for a file-based write-ahead log.
/// </summary>
public class FileWriteAheadLogSettings
{
    /// <summary>
    /// Gets or sets the path to the directory that holds the WAL.
    /// </summary>
    public string LogDirectoryPath { get; set; } = "./.wal";

    /// <summary>
    /// Gets or sets the size (in bytes) to reserve for new WAL files.
    /// </summary> 
    public long ReservedSize { get; set; } = 64 * 1024 * 1024; // 64 MB

    /// <summary>
    /// Gets or sets the buffer size (in bytes) to use for writing to WAL files.
    /// </summary> 
    public int BufferSize { get; set; } = 4 * 1024; // 4 KB
}
