using System;

namespace TeaSuite.KV.IO;

partial class Driver<TKey, TValue>
{
    /// <summary>
    /// Defines metadata for a segment.
    /// </summary>
    internal readonly record struct SegmentMetadata
    {
        /// <summary>
        /// The current version supported when writing segments.
        /// </summary>
        public const uint CurrentVersion = 1;

        /// <summary>
        /// Initializes a new instance of <see cref="SegmentMetadata"/>.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="SegmentFlags"/> value of the segment.
        /// </param>
        /// <param name="version">
        /// The version of the structure of the segment.
        /// </param>
        /// <param name="timestamp">
        /// A <see cref="DateTime"/> value indicating when the segment was written.
        /// </param>
        public SegmentMetadata(SegmentFlags flags, uint version, DateTime timestamp)
        {
            Version = version;
            Flags = flags;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SegmentMetadata"/> using the current values.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SegmentMetadata"/> using the current values.
        /// </returns>
        public static SegmentMetadata New()
        {
            SegmentFlags flags = SegmentFlags.None;

            if (BitConverter.IsLittleEndian)
            {
                flags |= SegmentFlags.LittleEndian;
            }

            return new SegmentMetadata(flags, CurrentVersion, DateTime.UtcNow);
        }

        /// <summary>
        /// Gets the <see cref="SegmentFlags"/> value of the segment.
        /// </summary>
        public SegmentFlags Flags { get; }

        /// <summary>
        /// Gets the version of the structure of the segment.
        /// </summary>
        public uint Version { get; }

        /// <summary>
        /// Gets a <see cref="DateTime"/> value indicating when the segment was written.
        /// </summary>
        public DateTime Timestamp { get; }
    }

    /// <summary>
    /// Defines flags for a segment.
    /// </summary>
    [Flags]
    internal enum SegmentFlags : UInt32
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// If set, indicates that the segment was written using little endian byte order. If unset, big endian byte
        /// order was used.
        /// </summary>
        /// <remarks>
        /// This bit is set both in the most significant byte and the least significant byte such that evaluation
        /// becomes independent of the machine's endianness.
        /// </remarks>
        LittleEndian = 0x01_00_00_01,
    }
}
