using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
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
}
