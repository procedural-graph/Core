// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Threading.Channels;

namespace ProceduralGraph.Generic
{
    internal static partial class TaskExtensions
    {
        private sealed class CallbackState : IEquatable<CallbackState>
        {
            public ILogger Logger { get; }

            public object? Context { get; }

            public CallbackState(ILogger logger, object? context)
            {
                Logger = logger;
                Context = context;
            }

            public void Deconstruct(out ILogger logger, out object? context)
            {
                logger = Logger;
                context = Context;
            }

            public bool Equals(CallbackState? other)
            {
                return other is { } && Logger.Equals(other.Logger) && Equals(Context, other.Context);
            }

            public override bool Equals(object? obj)
            {
                return obj is CallbackState other && Equals(other);
            }

            override public int GetHashCode()
            {
                return HashCode.Combine(Logger, Context);
            }

            public override string ToString()
            {
                return $"CallbackHandler {{ Logger = {Logger}, Context = {Context} }}";
            }
        }

        private static readonly UnboundedChannelOptions _channelOptions = new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        };

        public static IAsyncEnumerable<Task> WhenEach(this IEnumerable<Task> tasks)
        {
            IEnumerable<Task<object?>> projected = tasks.Select(AsObjectTask);
            return WhenEach(projected);
        }

        public static async IAsyncEnumerable<Task<T>> WhenEach<T>(IEnumerable<Task<T>> tasks, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Task<T>> taskList = tasks switch
            {
                IReadOnlyCollection<Task<T>> collection => collection,
                ICollection<Task<T>> collection => (IReadOnlyCollection<Task<T>>)collection,
                _ => tasks.ToList()
            };

            int remaining = taskList.Count;
            if (remaining == 0)
            {
                yield break;
            }

            Channel<Task<T>> channel = Channel.CreateUnbounded<Task<T>>(_channelOptions);

            void Write(Task<T> completedTask)
            {
                channel.Writer.TryWrite(completedTask);
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    channel.Writer.TryComplete();
                }
            }

            foreach (var task in taskList)
            {
                _ = task.ContinueWith(Write, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            IAsyncEnumerable<Task<T>> channelReader = channel.Reader.ReadAllAsync(cancellationToken);
            await foreach (Task<T>? completedTask in channelReader.ConfigureAwait(false))
            {
                yield return completedTask;
            }
        }

        private static async Task<object?> AsObjectTask(Task task)
        {
            await task.ConfigureAwait(false);
            return null;
        }

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
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
    }
}