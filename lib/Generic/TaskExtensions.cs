// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    internal static partial class TaskExtensions
    {
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

            var state = new CallbackState(logger, context);
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

        private static void OnTaskFaulted(Task task, object? state)
        {
            (ILogger logger, object? context) = (CallbackState)state!;
            if (task.Exception is { })
            {
                logger.LogException(task.Exception, context);
            }
        }
    }
}
