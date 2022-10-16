using System;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines per-entry flags in segments.
/// </summary>
[Flags]
internal enum EntryFlags : UInt32
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that the entry is deleted.
    /// </summary>
    Deleted = 0x80_00_00_00,
}
