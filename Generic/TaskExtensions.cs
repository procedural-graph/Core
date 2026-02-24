using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

internal static partial class TaskExtensions
{
    private static readonly ObjectPool<CallbackState> _callbackStatePool = new DefaultObjectPool<CallbackState>(new DefaultPooledObjectPolicy<CallbackState>());

    private sealed class CallbackState : IResettable
    {
        public ILogger? logger;
        public object? context;

        public bool TryReset()
        {
            logger = null;
            context = null;
            return true;
        }
    }

    private static readonly Action<Task, object?> LogOnFaultDelegate = OnTaskFaulted;

    public static async Task CancelAsync(this CancellationTokenSource cts, CancellationToken cancellationToken)
    {
        Task cancel = cts.CancelAsync();
        Task wait = cancel.WaitAsync(cancellationToken);
        await wait.ConfigureAwait(false);
    }

    public static void Forget(this Task task, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
    {
        const TaskContinuationOptions ContinuationOptions = TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously;

        if (task.IsCompleted)
        {
            if (task.IsFaulted)
            {
                logger.LogException(task.Exception!, context);
            }

            return;
        }

        CallbackState state = _callbackStatePool.Get();
        state.logger = logger;
        state.context = context;

        task.ContinueWith(LogOnFaultDelegate, state, cancellationToken, ContinuationOptions, TaskScheduler.Default);
    }

    public static void Forget(this ValueTask valueTask, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return;
        }

        Task task = valueTask.AsTask();
        task.Forget(logger, context, cancellationToken);
    }

    public static void Forget<T>(this ValueTask<T> valueTask, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return;
        }

        Task task = valueTask.AsTask();
        task.Forget(logger, context, cancellationToken);
    }

#if !NET8_0_OR_GREATER
    public static Task CancelAsync(this CancellationTokenSource cts)
    {
        if (cts is null)
        {
            throw new ArgumentNullException(nameof(cts));
        }

        return Task.Run(cts.Cancel);
    }

    /// <summary>
    /// Waits for the task to complete execution, monitoring the provided cancellation token.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that completes when the original task completes or when cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
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

    /// <summary>
    /// Waits for the task to complete execution, monitoring the provided cancellation token.
    /// </summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that completes when the original task completes or when cancellation is requested.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
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

    /// <summary>
    /// Waits for the task to complete execution within a specified timeout, monitoring the provided cancellation token.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that completes when the original task completes, timeout occurs, or cancellation is requested.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed before the task completed.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
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

    /// <summary>
    /// Waits for the task to complete execution within a specified timeout, monitoring the provided cancellation token.
    /// </summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    /// <returns>A task that completes when the original task completes, timeout occurs, or cancellation is requested.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed before the task completed.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
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
#endif

    private static void OnTaskFaulted(Task task, object? context)
    {
        CallbackState state = (CallbackState)context!;
        try
        {
            if (task.Exception is { })
            {
                state.logger!.LogException(task.Exception, state.context);
            }
        }
        finally
        {
            _callbackStatePool.Return(state);
        }
    }
}
