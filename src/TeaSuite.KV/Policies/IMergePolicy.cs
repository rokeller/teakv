namespace TeaSuite.KV.Policies;

/// <summary>
/// Defines the contract for a policy which defines when to merge segments.
/// </summary>
/// <remarks>
/// A large number of segments implies slower lookups for entries not currently found in the in-memory store: in such a
/// case, the most recent segment is searched first for a match, and then older segments are searched.
/// </remarks>
public interface IMergePolicy
{
    /// <summary>
    /// Determines if the existing segments should be merged into a new segment.
    /// </summary>
    /// <param name="segmentCount">
    /// A <see cref="long"/> value that defines how many segments there are currently.
    /// </param>
    /// <returns>
    /// True if the existing segments should be merged, or false otherwise.
    /// </returns>
    bool ShouldMerge(long segmentCount);
}
