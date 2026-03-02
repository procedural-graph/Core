#if !NET8_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections;

internal static class TaskExtensions
{
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (!cancellationToken.CanBeCanceled)
        {
            return task;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (task.IsCompleted)
        {
            return task;
        }

        return WaitAsyncCore(task, cancellationToken);
    }

    public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (!cancellationToken.CanBeCanceled)
        {
            return task;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        if (task.IsCompleted)
        {
            return task;
        }

        return WaitAsyncCore(task, cancellationToken);
    }

    private static async Task WaitAsyncCore(Task task, CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
        {
            var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);

            if (completedTask == tcs.Task)
            {
                await tcs.Task.ConfigureAwait(false);
            }

            await task.ConfigureAwait(false);
        }
    }

    private static async Task<T> WaitAsyncCore<T>(Task<T> task, CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
        {
            var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);

            if (completedTask == tcs.Task)
            {
                await tcs.Task.ConfigureAwait(false);
            }

            return await task.ConfigureAwait(false);
        }
    }
}
#endif