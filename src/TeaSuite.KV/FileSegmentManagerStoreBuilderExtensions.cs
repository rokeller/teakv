using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeaSuite.KV.IO;

namespace TeaSuite.KV;

/// <summary>
/// Provides extension methods for dependency injection to setup file-based storage for Key/Value store segments.
/// </summary>
public static class FileSegmentManagerStoreBuilderExtensions
{
    /// <summary>
    /// Adds file storage configured with the given <paramref name="configureOptions"/> action.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="configureOptions">
    /// An action to configure <see cref="FileSegmentsOptions"/> for the file segments.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    public static StoreBuilder<TKey, TValue> AddFileStorage<TKey, TValue>(
        this StoreBuilder<TKey, TValue> builder,
        Action<FileSegmentsOptions> configureOptions)
        where TKey : IComparable<TKey>
    {
        builder.Services
            .AddTransient<ISegmentManager<TKey, TValue>, FileSegmentManager<TKey, TValue>>()
            .ConfigureForStore<FileSegmentsOptions, TKey, TValue>(configureOptions)
            ;

        return builder;
    }

    /// <summary>
    /// Adds file storage configured with the given <paramref name="configuration"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration"/> to bind to the <see cref="FileSegmentsOptions"/>.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    public static StoreBuilder<TKey, TValue> AddFileStorage<TKey, TValue>(
        this StoreBuilder<TKey, TValue> builder,
        IConfiguration configuration)
        where TKey : IComparable<TKey>
    {
        return AddFileStorage(builder, configuration.Bind);
    }

    /// <summary>
    /// Adds file storage configured through the given <paramref name="newOptions"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="newOptions">
    /// The <see cref="FileSegmentsOptions"/> to use to configure the settings for file segments.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    public static StoreBuilder<TKey, TValue> AddFileStorage<TKey, TValue>(
        this StoreBuilder<TKey, TValue> builder,
        FileSegmentsOptions newOptions)
        where TKey : IComparable<TKey>
    {
        return AddFileStorage(builder, (settings) =>
        {
            settings.SegmentsDirectoryPath = newOptions.SegmentsDirectoryPath;
        });
    }

    #region Memory Mapped Files

    /// <summary>
    /// Adds memory-mapped file storage configured with the given <paramref name="configureOptions"/>
    /// action.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="configureOptions">
    /// An action to configure <see cref="FileSegmentsOptions"/> for the file segments.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    /// <remarks>
    /// Memory-mapped files are used only for reading of data segments.
    /// </remarks>
    public static StoreBuilder<TKey, TValue> AddMemoryMappedFileStorage<TKey, TValue>(
        this StoreBuilder<TKey, TValue> builder,
        Action<FileSegmentsOptions> configureOptions)
        where TKey : IComparable<TKey>
    {
        builder.Services
            .AddTransient<ISegmentManager<TKey, TValue>, MemoryMappedFileSegmentManager<TKey, TValue>>()
            .ConfigureForStore<FileSegmentsOptions, TKey, TValue>(configureOptions)
            ;

        return builder;
    }

    /// <summary>
    /// Adds memory-mapped file storage configured with the given <paramref name="configuration"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration"/> to bind to the <see cref="FileSegmentsOptions"/>.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    /// <remarks>
    /// Memory-mapped files are used only for reading of data segments.
    /// </remarks>
    public static StoreBuilder<TKey, TValue> AddMemoryMappedFileStorage<TKey, TValue>(
          this StoreBuilder<TKey, TValue> builder,
          IConfiguration configuration)
          where TKey : IComparable<TKey>
    {
        return AddMemoryMappedFileStorage(builder, configuration.Bind);
    }

    /// <summary>
    /// Adds memory-mapped file storage configured through the given <paramref name="newSettings"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="StoreBuilder{TKey, TValue}"/> to add file storage to.
    /// </param>
    /// <param name="newSettings">
    /// The <see cref="FileSegmentsOptions"/> to use to configure the settings for file segments.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of keys for entries of the store.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of values for entries of the store.
    /// </typeparam>
    /// <returns>
    /// The input <see cref="StoreBuilder{TKey, TValue}"/>.
    /// </returns>
    /// <remarks>
    /// Memory-mapped files are used only for reading of data segments.
    /// </remarks>
    public static StoreBuilder<TKey, TValue> AddMemoryMappedFileStorage<TKey, TValue>(
        this StoreBuilder<TKey, TValue> builder,
        FileSegmentsOptions newSettings)
        where TKey : IComparable<TKey>
    {
        return AddMemoryMappedFileStorage(builder, (settings) =>
        {
            settings.SegmentsDirectoryPath = newSettings.SegmentsDirectoryPath;
        });
    }

    #endregion
}
