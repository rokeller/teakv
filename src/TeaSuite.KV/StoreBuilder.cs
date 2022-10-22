using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeaSuite.KV.Data;

namespace TeaSuite.KV;

/// <summary>
/// Implements the builder patterns for Key/Value stores of <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TKey">
/// The type of keys for entries of the store.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of values for entries of the store.
/// </typeparam>
public class StoreBuilder<TKey, TValue> where TKey : IComparable<TKey>
{
    public StoreBuilder(IServiceCollection services)
    {
        Services = services
            .AddTransient<IMemoryKeyValueStoreFactory<TKey, TValue>, DefaultMemoryKeyValueStoreFactory<TKey, TValue>>()
            .AddOptions<StoreOptions<TKey, TValue>>()
            .Services
            ;
    }

    public virtual IServiceCollection Services { get; }

    /// <summary>
    /// Adds configuration for the <see cref="StoreSettings"/> for the store.
    /// </summary>
    /// <param name="configure">
    /// An action to configure <see cref="StoreSettings"/> for the store.
    /// </param>
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddStoreSettings(Action<StoreSettings> configure)
    {
        Services.Configure<StoreOptions<TKey, TValue>>((options) => configure(options.Settings));

        return this;
    }

    /// <summary>
    /// Configures the <see cref="StoreSettings"/> based on the given <paramref name="configuration"/> for the store.
    /// </summary>
    /// <param name="configuration"></param>
    /// The <see cref="IConfiguration"/> to bind to the <see cref="StoreSettings"/>.
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddStoreSettings(IConfiguration configuration)
    {
        return AddStoreSettings(configuration.Bind);
    }

    /// <summary>
    /// Configures the <see cref="StoreSettings"/> based on the given <paramref name="newSettings"/> for the store.
    /// </summary>
    /// <param name="newSettings">
    /// The <see cref="StoreSettings"/> to use to configure the settings of the store.
    /// </param>
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddStoreSettings(StoreSettings newSettings)
    {
        return AddStoreSettings((settings) =>
        {
            settings.IndexPolicy = newSettings.IndexPolicy;
            settings.MergePolicy = newSettings.MergePolicy;
            settings.PersistPolicy = newSettings.PersistPolicy;
        });
    }
}
