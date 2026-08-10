using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

/// <inheritdoc/>
public abstract class AsyncLifetime<TSelf> : Lifetime<TSelf> where TSelf : AsyncLifetime<TSelf>
{
    /// <summary>
    /// Represents an asynchronous completion stage in the lifetime of an object.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AsyncStage"/> class with the specified lifetime.
    /// </remarks>
    /// <inheritdoc/>
    public abstract class AsyncStage(TSelf lifetime) : Stage(lifetime), INotifyCompletion
    {
        private TaskCompletionSource? _tcs;
        private Task? _task;

        /// <inheritdoc/>
        public override bool IsCompleted => base.IsCompleted && Task.IsCompleted;

        internal Task Task
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _task) ?? InitTask(Lifetime);
        }

        /// <inheritdoc/>
        public void OnCompleted(Action continuation)
        {
            TaskAwaiter taskAwaiter = Task.GetAwaiter();
            taskAwaiter.OnCompleted(continuation);
        }

        /// <summary>
        /// Gets the result of the asynchronous operation represented by this lifetime stage.
        /// </summary>
        public void GetResult()
        {
            TaskAwaiter taskAwaiter = Task.GetAwaiter();
            taskAwaiter.GetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetTask([NotNullWhen(true)] out Task? task)
        {
            task = Volatile.Read(ref _task);
            return task is { };
        }

        /// <summary>
        /// Completes the current stage of the lifetime asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous completion operation.</returns>
        protected virtual Task ExecuteAsync()
        {
            for (Stage? syncStage = Previous; syncStage is { }; syncStage = syncStage.Previous)
            {
                if (syncStage is AsyncStage asyncStage)
                {
                    return asyncStage._task!;
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override void Complete()
        {
            Task complete = ExecuteAsync();

            if (Volatile.Read(ref _tcs) is { } tcs)
            {
                _ = complete.ContinueWith(tcs.SetFromTask, TaskContinuationOptions.ExecuteSynchronously);
                return;
            }

            Volatile.Write(ref _task, complete);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task InitTask(TSelf lifetime)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(lifetime);
#else
            if (lifetime is null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }
#endif

            lock (lifetime.SyncRoot)
            {
                if (TryGetTask(out Task? task))
                {
                    return task;
                }

                TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Volatile.Write(ref _tcs, tcs);
                task = tcs.Task;
                Volatile.Write(ref _task, task);

                return task;
            }
        }
    }
}
