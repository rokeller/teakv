using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TeaSuite.KV.IO.Formatters;

/// <summary>
/// Provies formatters (<see cref="IFormatter{T}"/>) for commonly used primitive types.
/// </summary>
public static partial class PrimitiveFormatters
{
    /// <summary>
    /// Registers implementations of <see cref="IFormatter{T}"/> for commonly used primitive types as transient services
    /// in dependency injection.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register the formatters.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> in which the formatters were registered.
    /// </returns>
    public static IServiceCollection AddPrimitiveFormatters(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IFormatter<sbyte>), typeof(SByteFormatter));
        services.TryAddTransient(typeof(IFormatter<byte>), typeof(ByteFormatter));
        services.TryAddTransient(typeof(IFormatter<short>), typeof(Int16Formatter));
        services.TryAddTransient(typeof(IFormatter<ushort>), typeof(UInt16Formatter));
        services.TryAddTransient(typeof(IFormatter<int>), typeof(Int32Formatter));
        services.TryAddTransient(typeof(IFormatter<uint>), typeof(UInt32Formatter));
        services.TryAddTransient(typeof(IFormatter<long>), typeof(Int64Formatter));
        services.TryAddTransient(typeof(IFormatter<ulong>), typeof(UInt64Formatter));

        services.TryAddTransient(typeof(IFormatter<bool>), typeof(BooleanFormatter));
        services.TryAddTransient(typeof(IFormatter<float>), typeof(SingleFormatter));
        services.TryAddTransient(typeof(IFormatter<double>), typeof(DoubleFormatter));
        services.TryAddTransient(typeof(IFormatter<decimal>), typeof(DecimalFormatter));
        services.TryAddTransient(typeof(IFormatter<Guid>), typeof(GuidFormatter));

        services.TryAddTransient(typeof(IFormatter<DateTime>), typeof(DateTimeFormatter));
        services.TryAddTransient(typeof(IFormatter<DateTimeOffset>), typeof(DateTimeOffsetFormatter));

        services.TryAddTransient(typeof(IFormatter<string>), typeof(StringFormatter));

        return services;
    }
}
