using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeaSuite.KV.IO.Formatters;

namespace TeaSuite.KV;

/// <summary>
/// Provides extension emthods for dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds a read-only singleton instance for <see cref="IKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register services.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the keys used for entries in the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values used for entries in the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="IServiceCollection"/>.
    /// </returns>
    public static StoreBuilder<TKey, TValue> AddReadOnlyKeyValueStore<TKey, TValue>(this IServiceCollection services)
        where TKey : IComparable<TKey>
    {
        return new StoreBuilder<TKey, TValue>(services
            .AddSingleton<IKeyValueStore<TKey, TValue>, ReadOnlyKeyValueStore<TKey, TValue>>()
            .AddDefaultFormatters()
            .AddSystemClock()
        );
    }

    /// <summary>
    /// Adds a singleton instance for <see cref="IKeyValueStore{TKey, TValue}"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register services.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the keys used for entries in the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values used for entries in the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="IServiceCollection"/>.
    /// </returns>
    public static StoreBuilder<TKey, TValue> AddKeyValueStore<TKey, TValue>(this IServiceCollection services)
        where TKey : IComparable<TKey>
    {
        return new StoreBuilder<TKey, TValue>(services
            .AddSingleton<IKeyValueStore<TKey, TValue>, DefaultKeyValueStore<TKey, TValue>>()
            .AddDefaultFormatters()
            .AddSystemClock()
        );
    }

    public static StoreBuilder<TKey, TValue> AddReadOnlyKeyValueStores<TKey, TValue, TSelectorKey>(
        this IServiceCollection services,
        string dynamicPathFormat)
        where TKey : IComparable<TKey>
    {
        return new StoreBuilder<TKey, TValue>(services
            .AddSingleton<IStoreSelector<TKey, TValue, TSelectorKey>, DefaultKeyValueStoreSelector<TKey, TValue, TSelectorKey>>()
            .AddTransient<IReadOnlyKeyValueStore<TKey, TValue>, ReadOnlyKeyValueStore<TKey, TValue>>()
            .ConfigureForStore<StoreSelectorOptions, TKey, TValue>(options => options.DynamicPathFormat = dynamicPathFormat)
            .AddDefaultFormatters()
            .AddSystemClock()
        );
    }

    public static StoreBuilder<TKey, TValue> AddKeyValueStores<TKey, TValue, TSelectorKey>(
        this IServiceCollection services,
        string dynamicPathFormat)
        where TKey : IComparable<TKey>
    {
        return new StoreBuilder<TKey, TValue>(services
            .AddSingleton<IStoreSelector<TKey, TValue, TSelectorKey>, DefaultKeyValueStoreSelector<TKey, TValue, TSelectorKey>>()
            .AddTransient<IKeyValueStore<TKey, TValue>, DefaultKeyValueStore<TKey, TValue>>()
            .ConfigureForStore<StoreSelectorOptions, TKey, TValue>(options => options.DynamicPathFormat = dynamicPathFormat)
            .AddDefaultFormatters()
            .AddSystemClock()
        );
    }

    /// <summary>
    /// Adds default formatters for <see cref="IEntryFormatter{TKey, TValue}"/> and <see cref="IFormatter{T}"/> for
    /// primitive types.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register services.
    /// </param>
    /// <returns>
    /// The input <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection AddDefaultFormatters(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IEntryFormatter<,>), typeof(DefaultEntryFormatter<,>));

        return services.AddPrimitiveFormatters();
    }

    /// <summary>
    /// Adds <typeparamref name="TFormatterImpl"/> as a formatter implementation for <see cref="IFormatter{T}"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register services.
    /// </param>
    /// <typeparam name="T">
    /// The type for which to add <see cref="IFormatter{T}"/>.
    /// </typeparam>
    /// <typeparam name="TFormatterImpl">
    /// The type implementing <see cref="IFormatter{T}"/>.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection AddFormatter<T, TFormatterImpl>(this IServiceCollection services)
        where TFormatterImpl : IFormatter<T>
    {
        return services.AddTransient(typeof(IFormatter<T>), typeof(TFormatterImpl));
    }

    private static IServiceCollection AddSystemClock(this IServiceCollection services)
    {
        services.TryAddSingleton<ISystemClock, SystemClock>();

        return services;
    }
}
