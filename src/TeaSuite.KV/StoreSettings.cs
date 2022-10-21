using System;
using TeaSuite.KV.Policies;

namespace TeaSuite.KV;

/// <summary>
/// Defines settings for a Key/Value store.
/// </summary>
public class StoreSettings
{
    /// <summary>
    /// Gets or sets the <see cref="IPersistPolicy"/> to use with the store.
    /// </summary>
    /// <remarks>
    /// Defaults to the default <see cref="DefaultPersistPolicy"/>.
    /// </remarks>
    public virtual IPersistPolicy PersistPolicy { get; set; } = new DefaultPersistPolicy();

    /// <summary>
    /// Gets or sets the <see cref="IIndexPolicy"/> to use with the store.
    /// </summary>
    /// <remarks>
    /// Defaults to the default <see cref="DefaultIndexPolicy"/>.
    /// </remarks>
    public virtual IIndexPolicy IndexPolicy { get; set; } = new DefaultIndexPolicy();

    /// <summary>
    /// Gets or sets the <see cref="IMergePolicy"/> to use with the store.
    /// </summary>
    /// <remarks>
    /// Defaults to the default <see cref="DefaultMergePolicy"/>.
    /// </remarks>
    public virtual IMergePolicy MergePolicy { get; set; } = new DefaultMergePolicy();

    /// <summary>
    /// Gets the minimum interval between persisting the in-memory store to segments.
    /// </summary>
    /// <remarks>
    /// Defaults to 10 seconds.
    /// </remarks>
    public virtual TimeSpan MinimumPersistInterval { get; set; } = TimeSpan.FromSeconds(10);
}
