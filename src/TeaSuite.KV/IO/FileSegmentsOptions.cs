namespace TeaSuite.KV.IO;

/// <summary>
/// Defines options for file-based persisted segments.
/// </summary>
public class FileSegmentsOptions
{
    /// <summary>
    /// Gets or sets the path to the directory that holds the segments.
    /// </summary>
    public string SegmentsDirectoryPath { get; set; } = "./teadb";
}
