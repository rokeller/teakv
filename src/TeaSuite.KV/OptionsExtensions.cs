using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TeaSuite.KV;

/// <summary>
/// Extensions for options as used in the context of a Key/Value store.
/// </summary>
public static class OptionsExtensions
{
    /// <summary>
    /// Configures options for a Key/Value store.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> in which to register services.
    /// </param>
    /// <param name="configureOptions">
    /// An action that configures an instance of <typeparamref name="TOptions"/>.
    /// </param>
    /// <typeparam name="TOptions">
    /// The type of the options to configure.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection ConfigureForStore<TOptions, TKey, TValue>(
        this IServiceCollection services,
        Action<TOptions> configureOptions)
        where TOptions : class
    {
        return services
            .Configure<TOptions>(GetOptionsName<TKey, TValue>(), configureOptions)
            ;
    }

    /// <summary>
    /// Gets a <typeparamref name="TOptions"/> instances for the typed Key/Value store from the given
    /// <paramref name="options"/>.
    /// </summary>
    /// <param name="options">
    /// The <see cref="IOptionsMonitor{TOptions}"/> from which to get the options.
    /// </param>
    /// <typeparam name="TOptions">
    /// The type of the options to get.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// A <typeparamref name="TOptions"/> instance.
    /// </returns>
    public static TOptions GetForStore<TOptions, TKey, TValue>(this IOptionsMonitor<TOptions> options)
        where TOptions : class
    {
        return options.Get(GetOptionsName<TKey, TValue>());
    }

    /// <summary>
    /// Gets the name for the named options of a Key/Value store.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// A name for named options.
    /// </returns>
    public static string GetOptionsName<TKey, TValue>()
    {
        return $"KVStore<{typeof(TKey).Name},{typeof(TValue).Name}>";
    }
}
