using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeaSuite.KV;

partial class StoreBuilder<TKey, TValue>
{
    /// <summary>
    /// Adds and configures the file-based write-ahead log to use with the store.
    /// </summary>
    /// <param name="configure">
    /// An action to configure <see cref="FileWriteAheadLogSettings"/> for
    /// the file-based write-ahead log used with the store.
    /// </param>
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddWriteAheadLog(
        Action<FileWriteAheadLogSettings> configure)
    {
        Services
            .Configure(OptionsExtensions.GetOptionsName<TKey, TValue>(), configure)
            .AddSingleton<IWriteAheadLog<TKey, TValue>, FileWriteAheadLog<TKey, TValue>>()
            ;

        return this;
    }

    /// <summary>
    /// Adds and configures the file-based write-ahead log to use with the store.
    /// </summary>
    /// <param name="configuration"></param>
    /// The <see cref="IConfiguration"/> to bind to the <see cref="FileWriteAheadLogSettings"/>
    /// for the file-based write-ahead log used with the store.
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddWriteAheadLog(
        IConfiguration configuration)
    {
        return AddWriteAheadLog(configuration.Bind);
    }

    /// <summary>
    /// Adds and configures the file-based write-ahead log to use with the store.
    /// </summary>
    /// <param name="newSettings">
    /// The <see cref="FileWriteAheadLogSettings"/> to use to configure the
    /// file-based write-ahead log used with the store.
    /// </param>
    /// <returns>
    /// The current instance.
    /// </returns>
    public virtual StoreBuilder<TKey, TValue> AddWriteAheadLog(
        FileWriteAheadLogSettings newSettings)
    {
        return AddWriteAheadLog((settings) =>
        {
            settings.LogDirectoryPath = newSettings.LogDirectoryPath;
            settings.ReservedSize = newSettings.ReservedSize;
            settings.BufferSize = newSettings.BufferSize;
        });
    }
}
