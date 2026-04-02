#if !NET8_0_OR_GREATER
using System.Threading.Tasks;

namespace System.Threading;

internal static class TaskExtensions
{
    public static async Task CancelAsync(this CancellationTokenSource cts, CancellationToken cancellationToken)
    {
        Task cancel = cts.CancelAsync();
        Task wait = cancel.WaitAsync(cancellationToken);
        await wait.ConfigureAwait(false);
    }

    public static Task CancelAsync(this CancellationTokenSource cts)
    {
        if (cts is null)
        {
            throw new ArgumentNullException(nameof(cts));
        }

        return Task.Run(cts.Cancel);
    }

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

    public static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (task.IsCompleted)
        {
            return task;
        }

        return timeout == Timeout.InfiniteTimeSpan ? task.WaitAsync(cancellationToken) : WaitAsyncWithTimeoutCore(task, timeout, cancellationToken);
    }

    public static Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        if (task.IsCompleted)
        {
            return task;
        }

        return timeout == Timeout.InfiniteTimeSpan ? task.WaitAsync(cancellationToken) : WaitAsyncWithTimeoutCore(task, timeout, cancellationToken);
    }

    private static async Task WaitAsyncWithTimeoutCore(Task task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(timeout, cts.Token);
        Task completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            throw new TimeoutException();
        }

        cts.Cancel();

        await task.ConfigureAwait(false);
    }

    private static async Task<T> WaitAsyncWithTimeoutCore<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(timeout, cts.Token);
        Task completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

        if (completedTask == delayTask)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            throw new TimeoutException();
        }

        cts.Cancel();

        return await task.ConfigureAwait(false);
    }
}
#endif