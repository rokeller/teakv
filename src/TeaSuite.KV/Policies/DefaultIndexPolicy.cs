namespace TeaSuite.KV.Policies;

/// <summary>
/// A default implementation of the <see cref="IIndexPolicy"/>.
/// </summary>
public readonly struct DefaultIndexPolicy : IIndexPolicy
{
    private readonly long afterBytes;
    private readonly long afterEntries;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultIndexPolicy"/>. This would index entries after at most 2048
    /// bytes in the data file, or after at most 100 entries.
    /// </summary>
    public DefaultIndexPolicy() : this(2048, 100) { }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultIndexPolicy"/>, using the given parameters.
    /// </summary>
    /// <param name="afterBytes">
    /// The offset in bytes in the data file after which to index the next entry.
    /// </param>
    /// <param name="afterEntries">
    /// The offset in entries since after which to index the next entry..
    /// </param>
    public DefaultIndexPolicy(long afterBytes, long afterEntries)
    {
        this.afterBytes = afterBytes;
        this.afterEntries = afterEntries;
    }

    /// <inheritdoc/>
    public bool ShouldIndex(long bytesOffsetFromLast, long entriesOffsetFromLast, long entryIndex)
    {
        return bytesOffsetFromLast >= afterBytes || entriesOffsetFromLast >= afterEntries;
    }
}
