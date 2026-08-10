global using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource<object?>;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameSharp;

internal static class TaskCompletionSourceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetResult(this TaskCompletionSource tcs)
    {
        tcs.SetResult(null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetResult(this TaskCompletionSource tcs)
    {
        return tcs.TrySetResult(null);
    }

    public static void SetFromTask<TResult>(this TaskCompletionSource<TResult> tcs, Task<TResult> completedTask)
    {
        if (!TrySetFromTask(tcs, completedTask))
        {
            throw new ArgumentException("Task is already completed.", nameof(completedTask));
        }
    }

    public static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> tcs, Task<TResult> completedTask)
    {
        if (completedTask is null)
        {
            throw new ArgumentNullException(nameof(completedTask));
        }

        return completedTask.Status switch
        {
            TaskStatus.RanToCompletion => tcs.TrySetResult(completedTask.GetAwaiter().GetResult()),
            TaskStatus.Canceled => tcs.TrySetCanceled(),
            TaskStatus.Faulted => tcs.TrySetException(completedTask.Exception!),
            _ => throw new ArgumentException("Task must be completed.", nameof(completedTask))
        };
    }

    public static void SetFromTask(this TaskCompletionSource tcs, Task completedTask)
    {
        if (!TrySetFromTask(tcs, completedTask))
        {
            throw new ArgumentException("Task is already completed.", nameof(completedTask));
        }
    }

    public static bool TrySetFromTask(this TaskCompletionSource tcs, Task completedTask)
    {
        if (completedTask is null)
        {
            throw new ArgumentNullException(nameof(completedTask));
        }

        return completedTask.Status switch
        {
            TaskStatus.RanToCompletion => tcs.TrySetResult(null),
            TaskStatus.Canceled => tcs.TrySetCanceled(),
            TaskStatus.Faulted => tcs.TrySetException(completedTask.Exception!),
            _ => throw new ArgumentException("Task must be completed.", nameof(completedTask))
        };
    }
}
