using System.Collections.Generic;

namespace TeaSuite.KV.IO;

/// <summary>
/// Defines options for dynamic paths.
/// </summary>
public class DynamicPathOptions
{
    /// <summary>
    /// Gets an <see cref="IList{T}"/> of string values representing segments of paths.
    /// </summary>
    public IList<string> PathSegments { get; } = new List<string>();
}
