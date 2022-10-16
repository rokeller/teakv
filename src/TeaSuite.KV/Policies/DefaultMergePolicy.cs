namespace TeaSuite.KV.Policies;

/// <summary>
/// A default implementation of the <see cref="IMergePolicy"/>.
/// </summary>
public readonly struct DefaultMergePolicy : IMergePolicy
{
    private readonly long minSegmentCount;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultMergePolicy"/>. This would ask that as soon as at least 2
    /// segments exist, they be merged.
    /// </summary>
    public DefaultMergePolicy() : this(2) { }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultIndexPolicy"/> using the given parameters.
    /// </summary>
    /// <param name="minSegmentCount">
    /// A <see cref="long"/> value that defines the minimum number of segments for which a merge is scheduled.
    /// </param>
    public DefaultMergePolicy(long minSegmentCount)
    {
        this.minSegmentCount = minSegmentCount;
    }

    /// <inheritdoc/>
    public bool ShouldMerge(long segmentCount)
    {
        return segmentCount >= minSegmentCount;
    }
}
