#if NETSTANDARD
using System;

namespace TeaSuite.KV;

/// <summary>
/// Helper extension methods for functionality missing in netstandard2.0.
/// </summary>
internal static class NetStandardUtils
{
    internal static T Get<T>(this ArraySegment<T> seg, int index)
    {
        return seg.Array[seg.Offset + index];
    }

    internal static ArraySegment<T> Slice<T>(this ArraySegment<T> seg, int index)
    {
        return new ArraySegment<T>(seg.Array, seg.Offset + index, seg.Count - index);
    }

    internal static ArraySegment<T> Slice<T>(this ArraySegment<T> seg, int index, int count)
    {
        return new ArraySegment<T>(seg.Array, seg.Offset + index, count);
    }
}
#endif
