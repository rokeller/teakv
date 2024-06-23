using System;

namespace TeaSuite.KV;

/// <summary>
/// Implements <see cref="IDisposable"/> doing nothing.
/// /// </summary>
public readonly struct NullDisposable : IDisposable
{
    /// <summary>
    /// The default instance of <see cref="NullDisposable"/>.
    /// </summary>
    public static readonly NullDisposable Instance = new();

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
