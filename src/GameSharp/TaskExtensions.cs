using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp;

/// <summary>
/// Provides extension methods for the <see cref="Task"/> class to allow for strongly-typed state objects in continuation methods.
/// </summary>
public static class TaskExtensions
{
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith(Action{Task, object}, object, CancellationToken, TaskContinuationOptions, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ContinueWith<TState>(
        this Task task, 
        Action<Task, TState> continuationAction, 
        TState state, 
        CancellationToken cancellationToken, 
        TaskContinuationOptions continuationOptions, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Action<Task, object?>>(continuationAction), 
            state, cancellationToken, continuationOptions, scheduler);
    }

    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith(Action{Task, object}, object, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ContinueWith<TState>(
        this Task task, 
        Action<Task, TState> continuationAction, 
        TState state, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Action<Task, object?>>(continuationAction), state, scheduler);
    }

    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith(Action{Task, object}, object, TaskContinuationOptions)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ContinueWith<TState>(
        this Task task, 
        Action<Task, TState> continuationAction, 
        TState state, 
        TaskContinuationOptions continuationOptions) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Action<Task, object?>>(continuationAction), state, continuationOptions);
    }

    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith(Action{Task, object}, object, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ContinueWith<TState>(
        this Task task, 
        Action<Task, TState> continuationAction, 
        TState state, 
        CancellationToken cancellationToken) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Action<Task, object?>>(continuationAction), state, cancellationToken);
    }

    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith(Action{Task, object}, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ContinueWith<TState>(
        this Task task, 
        Action<Task, TState> continuationAction, 
        TState state) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Action<Task, object?>>(continuationAction), state);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TResult> continuationFunction, 
        TState state, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TResult>>(continuationFunction), state, scheduler);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, CancellationToken, TaskContinuationOptions, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken, 
        TaskContinuationOptions continuationOptions, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TResult>>(continuationFunction), state, cancellationToken, continuationOptions, scheduler);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TResult>>(continuationFunction), state, cancellationToken);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, TaskContinuationOptions)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TResult> continuationFunction, 
        TState state, 
        TaskContinuationOptions continuationOptions) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TResult>>(continuationFunction), state, continuationOptions);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TResult> continuationFunction, 
        TState state) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TResult>>(continuationFunction), state);
    }

    /// <typeparam name="TResult">The type of the result produced by the continuation.</typeparam>
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith{TResult}(Func{Task, object, TResult}, object, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TNewResult> ContinueWith<TResult, TState, TNewResult>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TNewResult> continuationFunction, 
        TState state, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TNewResult>>(continuationFunction), state, scheduler);
    }

    /// <typeparam name="TResult">The type of the result produced by the continuation.</typeparam>
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith{TResult}(Func{Task, object, TResult}, object, CancellationToken, TaskContinuationOptions, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TNewResult> ContinueWith<TResult, TState, TNewResult>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TNewResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken, 
        TaskContinuationOptions continuationOptions, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TNewResult>>(continuationFunction), state, cancellationToken, continuationOptions, scheduler);
    }

    /// <typeparam name="TResult">The type of the result produced by the continuation.</typeparam>
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith{TResult}(Func{Task, object, TResult}, object, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TNewResult> ContinueWith<TResult, TState, TNewResult>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TNewResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TNewResult>>(continuationFunction), state, cancellationToken);
    }

    /// <typeparam name="TResult">The type of the result produced by the continuation.</typeparam>
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith{TResult}(Func{Task, object, TResult}, object, TaskContinuationOptions)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TNewResult> ContinueWith<TResult, TState, TNewResult>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TNewResult> continuationFunction, 
        TState state, 
        TaskContinuationOptions continuationOptions) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TNewResult>>(continuationFunction), state, continuationOptions);
    }

    /// <typeparam name="TResult">The type of the result produced by the continuation.</typeparam>
    /// <typeparam name="TState">The type of the state object passed to the continuation.</typeparam>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <inheritdoc cref="Task.ContinueWith{TResult}(Func{Task, object, TResult}, object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TNewResult> ContinueWith<TResult, TState, TNewResult>(
        this Task<TResult> task, 
        Func<Task<TResult>, TState, TNewResult> continuationFunction, 
        TState state) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task<TResult>, object?, TNewResult>>(continuationFunction), state);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task task, 
        Func<Task, TState, TResult> continuationFunction, 
        TState state, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task, object?, TResult>>(continuationFunction), state, scheduler);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, CancellationToken, TaskContinuationOptions, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task task, 
        Func<Task, TState, TResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken, 
        TaskContinuationOptions continuationOptions, 
        TaskScheduler scheduler) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task, object?, TResult>>(continuationFunction), state, cancellationToken, continuationOptions, scheduler);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task task, 
        Func<Task, TState, TResult> continuationFunction, 
        TState state, 
        CancellationToken cancellationToken) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task, object?, TResult>>(continuationFunction), state, cancellationToken);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState, TaskContinuationOptions)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task task, 
        Func<Task, TState, TResult> continuationFunction, 
        TState state, 
        TaskContinuationOptions continuationOptions) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task, object?, TResult>>(continuationFunction), state, continuationOptions);
    }

    /// <inheritdoc cref="ContinueWith{TResult, TState, TNewResult}(Task{TResult}, Func{Task{TResult}, TState, TNewResult}, TState)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TResult> ContinueWith<TResult, TState>(
        this Task task, 
        Func<Task, TState, TResult> continuationFunction, 
        TState state) 
        where TState : class?
    {
        return task.ContinueWith(Unsafe.As<Func<Task, object?, TResult>>(continuationFunction), state);
    }
}
