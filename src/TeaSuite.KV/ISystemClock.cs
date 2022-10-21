using System;
namespace TeaSuite.KV;

/// <summary>
/// An abstraction for the system clock.
/// </summary>
public interface ISystemClock
{
    /// <summary>
    /// Gets the current system time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
