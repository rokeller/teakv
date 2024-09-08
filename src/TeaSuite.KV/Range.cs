using System;

namespace TeaSuite.KV;

/// <summary>
/// Defines a range with an optional inclusive start and an optional exclusive
/// end.
/// </summary>
public readonly record struct Range<T> where T : IComparable<T>
{
    /// <summary>
    /// The default i.e. unbounded range.
    /// </summary>
    public static readonly Range<T> Unbounded = new();

    /// <summary>
    /// Gets or sets a flag that indicates whether the <see cref="Start"/>
    /// property defines a valid inclusive start.
    /// </summary>
    public bool HasStart { get; init; }

    /// <summary>
    /// Gets or sets the inclusive start of the range. This must be ignored
    /// unless <see cref="HasStart"/> is set to <c>true</c>.
    /// </summary>
    public T Start { get; init; }

    /// <summary>
    /// Gets or sets a flag that indicates whether the <see cref="End"/>
    /// property defines a valid exclusive end.
    /// </summary>
    public bool HasEnd { get; init; }

    /// <summary>
    /// Gets or sets the exclusive end of the range. This must be ignored unless
    /// <see cref="HasEnd"/> is set to <c>true</c>.
    /// </summary>
    public T End { get; init; }

    /// <summary>
    /// Gets a flag which indicates whether this range is bounded, i.e. either a
    /// the inclusive start or the exclusive end (or both) are defined.
    /// </summary>
    public bool IsBounded { get { return HasStart || HasEnd; } }
}
