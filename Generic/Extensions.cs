using Microsoft.Extensions.ObjectPool;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
#if NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
#endif

namespace ProceduralGraph.Generic;

/// <summary>
/// Provides extension methods for various types used in the procedural graph framework.
/// </summary>
public static class Extensions
{
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

    private static readonly ObjectPool<CallbackState> _callbackStatePool = new DefaultObjectPool<CallbackState>(new DefaultPooledObjectPolicy<CallbackState>());

    private static readonly Action<Task, object?> _logOnFaultDelegate = OnTaskFaulted;

    internal static async Task CancelAsync(this CancellationTokenSource cts, CancellationToken cancellationToken)
    {
        Task cancel = cts.CancelAsync();
        Task wait = cancel.WaitAsync(cancellationToken);
        await wait.ConfigureAwait(false);
    }

    internal static void Forget(this Task task, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
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

        task.ContinueWith(_logOnFaultDelegate, state, cancellationToken, ContinuationOptions, TaskScheduler.Default);
    }

    internal static void Forget(this ValueTask valueTask, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return;
        }

        Task task = valueTask.AsTask();
        task.Forget(logger, context, cancellationToken);
    }

    internal static void Forget<T>(this ValueTask<T> valueTask, ILogger logger, object? context = default, CancellationToken cancellationToken = default)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            return;
        }

        Task task = valueTask.AsTask();
        task.Forget(logger, context, cancellationToken);
    }

    /// <summary>
    /// Spawns a new scene member using the specified manager and parent.
    /// </summary>
    /// <param name="manager">The scene member manager.</param>
    /// <param name="parent">The parent scene member.</param>
    /// <returns>A handle to the newly spawned scene member.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="manager"/> is null.</exception>
    /// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneMemberHandle<TSceneMember> Spawn<TSceneMember>(this ISceneMemberManager<TSceneMember> manager, TSceneMember parent) 
        where TSceneMember : class
    {
        ThrowHelpers.ThrowIfNull(manager, nameof(manager));
        return new SceneMemberHandle<TSceneMember>(parent, manager);
    }

#if NETFRAMEWORK
    internal static bool TryPop<T>(this Stack<T> stack, [NotNullWhen(true)] out T? result) where T : notnull
    {
        if (stack.Count > 0)
        {
            result = stack.Pop();
            return true;
        }

        result = default;
        return false;
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
