using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV;

internal static class TaskExtensions
{
    public static ConfiguredValueTaskAwaitable ConfigureAwaitLib(this ValueTask valueTask)
    {
        return valueTask.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static void GetValueTaskResult(this ValueTask valueTask)
    {
        if (!valueTask.IsCompletedSuccessfully)
        {
            valueTask.ConfigureAwaitLib().GetAwaiter().GetResult();
        }
    }

    public static ConfiguredValueTaskAwaitable<T> ConfigureAwaitLib<T>(this ValueTask<T> valueTask)
    {
        return valueTask.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static T GetValueTaskResult<T>(this ValueTask<T> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return valueTask.Result;
        }
        else
        {
            return valueTask.ConfigureAwaitLib().GetAwaiter().GetResult();
        }
    }

    public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwaitLib<T>(this IAsyncEnumerable<T> source)
    {
        return source.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static ConfiguredTaskAwaitable ConfigureAwaitLib(this Task task)
    {
        return task.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static void GetTaskResult(this Task task)
    {
        task.ConfigureAwaitLib().GetAwaiter().GetResult();
    }

    public static ConfiguredTaskAwaitable<T> ConfigureAwaitLib<T>(this Task<T> task)
    {
        return task.ConfigureAwait(continueOnCapturedContext: false);
    }

    public static Task ContinueWith(this Task task, Func<Task> continuation)
    {
        return task.ContinueWith(_ => continuation()).Unwrap();
    }

    public static Task ContinueWith(this Task task, Func<CancellationToken, Task> continuation)
    {
        return task.ContinueWith(_ => continuation(default)).Unwrap();
    }
}
