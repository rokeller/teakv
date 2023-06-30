#if NETSTANDARD

// Needed for accessors with `init` when targeting .Net Standard:

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

#endif
