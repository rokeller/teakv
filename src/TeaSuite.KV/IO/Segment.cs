using System;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines a segment in the Key/Value store.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used for entries in the segment.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value used for entries in the segment.
/// </typeparam>
public readonly record struct Segment<TKey, TValue> : IComparable<Segment<TKey, TValue>> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Gets a <see cref="long"/> value that uniquely identifies the segment.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets a <see cref="Driver{TKey, TValue}"/> instance that is used to read data from the segment.
    /// </summary>
    public Driver<TKey, TValue> Driver { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="Segment{TKey, TValue}"/>.
    /// </summary>
    /// <param name="segmentId">
    /// The <see cref="long"/> value that uniquely identifies the segment.
    /// </param>
    /// <param name="driver">
    /// The <see cref="Driver{TKey, TValue}"/> instance that is used to read data from the segment.
    /// </param>
    public Segment(long segmentId, Driver<TKey, TValue> driver)
    {
        Id = segmentId;
        Driver = driver;
    }

    /// <inheritdoc/>
    public int CompareTo(Segment<TKey, TValue> other)
    {
        return -Id.CompareTo(other.Id);
    }
}
