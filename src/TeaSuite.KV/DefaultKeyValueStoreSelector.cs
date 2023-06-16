using System.Collections.Concurrent;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using TeaSuite.KV.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TeaSuite.KV;

public class DefaultKeyValueStoreSelector<TKey, TValue, TSelectorKey> :
    IStoreSelector<TKey, TValue, TSelectorKey>, IDisposable
    where TKey : IComparable<TKey>
{
    private readonly ConcurrentDictionary<TSelectorKey, ScopedStore> instanceMap =
        new ConcurrentDictionary<TSelectorKey, ScopedStore>();

    private readonly IServiceProvider services;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly StoreSelectorOptions options;
    private bool isDisposed;

    public DefaultKeyValueStoreSelector(
        IOptionsMonitor<StoreSelectorOptions> options,
        IServiceProvider services,
        IServiceScopeFactory scopeFactory)
    {
        this.options = options.GetForStore<StoreSelectorOptions, TKey, TValue>();
        this.services = services;
        this.scopeFactory = scopeFactory;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                List<Task> pendingDispose = new List<Task>(instanceMap.Count);

                foreach (ScopedStore scopedStore in instanceMap.Values)
                {
                    pendingDispose.Add(Task.Run(scopedStore.Dispose));
                }

                Task.WhenAll(pendingDispose).GetTaskResult();
            }

            isDisposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DefaultTeaDbSelector()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public IReadOnlyKeyValueStore<TKey, TValue> Select(TSelectorKey selectorKey)
    {
        ScopedStore scope = instanceMap.GetOrAdd(selectorKey, ScopeFactory);

        return scope.Store;
    }

    private ScopedStore ScopeFactory(TSelectorKey selectorKey)
    {
        IServiceScope serviceScope = scopeFactory.CreateScope();

        DynamicPathOptions scopeDynamicPathOptions = serviceScope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<DynamicPathOptions>>()
            .GetForStore<DynamicPathOptions, TKey, TValue>();

        scopeDynamicPathOptions.PathSegments.Add(String.Format(options.DynamicPathFormat, selectorKey));

        return new ScopedStore(serviceScope);
    }

    private readonly struct ScopedStore : IDisposable
    {
        private readonly IServiceScope serviceScope;
        private readonly IReadOnlyKeyValueStore<TKey, TValue> store;

        public ScopedStore(IServiceScope serviceScope)
        {
            this.serviceScope = serviceScope;
            this.store = serviceScope.ServiceProvider.GetRequiredService<IReadOnlyKeyValueStore<TKey, TValue>>();
        }

        public IReadOnlyKeyValueStore<TKey, TValue> Store => store;

        public void Dispose()
        {
            this.serviceScope.Dispose();
        }

        // public ValueTask DisposeAsync()
        // {
        //     serviceScope.Dispose();

        //     return default;
        // }
    }
}
