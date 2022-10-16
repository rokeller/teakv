namespace TeaSuite.KV.Policies;

/// <summary>
/// Defines the contract for a policy that defines which entries of a segment to index.
/// </summary>
/// <remarks>
/// <para>
/// This policy directly influences how big a segment's index file will get, but also how fast entries can be found in
/// persisted segments. The best policy is typically determined by the application itself, and can be found through
/// experimenting.
/// </para>
/// 
/// <para>
/// It is <b>not</b> necessary for a policy to mark the first and the last entry to be indexed, as they are always
/// indexed automatically.
/// </para>
/// </remarks>
public interface IIndexPolicy
{
    /// <summary>
    /// Determines if an entry in a segment's data file should also get an entry in the index file.
    /// </summary>
    /// <param name="bytesOffsetFromLast">
    /// The offset in bytes in the data file since the last indexed entry.
    /// </param>
    /// <param name="entriesOffsetFromLast">
    /// The offset in entries since the last indexed entry.
    /// </param>
    /// <param name="entryIndex">
    /// The index of the current entry to be written.
    /// </param>
    /// <returns>
    /// True if the current entry should be indexed, or false otherwise.
    /// </returns>
    bool ShouldIndex(long bytesOffsetFromLast, long entriesOffsetFromLast, long entryIndex);
}
