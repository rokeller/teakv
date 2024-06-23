using System;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV;

partial class FileWriteAheadLog<TKey, TValue>
{
    /// <summary>
    /// The <see cref="SemaphoreSlim"/> that's used to guard WAL operations.
    /// </summary>
    private readonly SemaphoreSlim walGuard = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Asynchronously starts a new guarded operation.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <see cref="GuardCompletion"/> that
    /// completes the guarded operation when disposed.
    /// </returns>
    private async ValueTask<GuardCompletion> StartGuardAsync()
    {
        await walGuard.WaitAsync().ConfigureAwaitLib();
        return new GuardCompletion(walGuard);
    }

    /// <summary>
    /// Starts a new guarded operation.
    /// </summary>
    /// <returns>
    /// A <see cref="GuardCompletion"/> value that completes the guarded
    /// operation when disposed.
    /// </returns>
    private GuardCompletion StartGuard()
    {
        walGuard.Wait();
        return new GuardCompletion(walGuard);
    }

    /// <summary>
    /// A simple struct implementing <see cref="IDisposable"/> to release the
    /// <paramref name="Guard"/> when disposed.
    /// </summary>
    private readonly record struct GuardCompletion(
        SemaphoreSlim Guard
        ) : IDisposable
    {
        public void Dispose()
        {
            Guard.Release();
        }
    }
}
