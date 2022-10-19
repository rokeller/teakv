using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines the contract for a manager of segments that can discover existing segments, create new segments, and delete
/// segments.
/// </summary>
/// <typeparam name="TKey">
/// The type of the keys used in the segments.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the values used in the segments.
/// </typeparam>
public interface ISegmentManager<TKey, TValue> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Creates a new writable segment with the given <paramref name="segmentId"/>.
    /// </summary>
    /// <param name="segmentId">
    /// A <see cref="long"/> value that uniquely identifies the segment.
    /// </param>
    /// <returns>
    /// An instance of <see cref="Segment{TKey, TValue}"/> that represents the new (writable) segment.
    /// </returns>
    Segment<TKey, TValue> CreateNewSegment(long segmentId);

    /// <summary>
    /// Deletes the existing segment with the given <paramref name="segmentId"/>.
    /// </summary>
    /// <param name="segmentId">
    /// The <see cref="long"/> value that uniquely identifies the segment to delete.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> value that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that tracks completion of the operation.
    /// </returns>
    ValueTask DeleteSegmentAsync(long segmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a read-only version of the given <paramref name="segment"/>.
    /// </summary>
    /// <param name="segment">
    /// The <see cref="Segment{TKey, TValue}"/> of which to get a read-only version.
    /// </param>
    /// <returns>
    /// An instance of <see cref="Segment{TKey, TValue}"/> that can be used to read from the given <paramref name="segment"/>.
    /// </returns>
    Segment<TKey, TValue> MakeReadOnly(Segment<TKey, TValue> segment);

    /// <summary>
    /// Discovers and enumerates all currently known segments.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="Segment{TKey, TValue}"/> that enumerates all currently known segments.
    /// </returns>
    IEnumerable<Segment<TKey, TValue>> DiscoverSegments();
}
