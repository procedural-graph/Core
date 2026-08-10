using GameSharp.Collections;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProceduralGraph;

internal static class TaskExtensions
{
    public static Task DisposeAsync(this ReadOnlyTypeLookup lookup)
    {
        int count = 0;
        ref Task task = ref Rent(16, out Task[] taskArray);

        foreach (IAsyncDisposable asyncDisposable in lookup.GetAll<IAsyncDisposable>())
        {
            ValueTask disposal = asyncDisposable.DisposeAsync();

            if (disposal.IsCompletedSuccessfully)
            {
                continue;
            }

            int index = count++;

            if (count > taskArray.Length)
            {
                task = ref Grow(count, ref taskArray);
            }

            Unsafe.Add(ref task, index) = disposal.AsTask();
        }

        if (count > 0)
        {
            return WaitReturnAndDisposeAsync(lookup, taskArray, count);
        }

        ArrayPool<Task>.Shared.Return(taskArray);
        Dispose(lookup);
        return Task.CompletedTask;
    }

    public static void Dispose(this ReadOnlyTypeLookup lookup)
    {
        int count = 0;
        ref Exception exception = ref Rent(16, out Exception[] exceptionArray);

        foreach (IDisposable disposable in lookup.GetAll<IDisposable>())
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                int index = count++;

                if (count > exceptionArray.Length)
                {
                    exception = ref Grow(count, ref exceptionArray);
                }

                Unsafe.Add(ref exception, index) = ex;
            }
        }

        switch (count)
        {
            case 0: ArrayPool<Exception>.Shared.Return(exceptionArray, clearArray: true); break;
            case 1: ReturnAndThrow(exceptionArray, exception); break;
            default: ReturnAndThrow(exceptionArray, new AggregateException(exceptionArray[..count])); break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
    private static void ReturnAndThrow(Exception[] array, Exception ex)
    {
        ArrayPool<Exception>.Shared.Return(array, clearArray: true);
        Throw(ex);
    }

    private static async Task WaitReturnAndDisposeAsync(ReadOnlyTypeLookup lookup, Task[] taskArray, int count)
    {
        Task wait = Task.WhenAll(taskArray.AsSpan(0, count));
        await wait.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ArrayPool<Task>.Shared.Return(taskArray, clearArray: true);
        DisposeAndAggregateExceptions(lookup, wait);
        if (!wait.IsCompletedSuccessfully)
        {
            Throw(wait.Exception!);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DisposeAndAggregateExceptions(ReadOnlyTypeLookup lookup, Task wait)
    {
        try
        {
            Dispose(lookup);
        }
        catch (Exception ex) when (!wait.IsCompletedSuccessfully)
        {
            if (wait.Exception is AggregateException aggEx1)
            {
                if (ex is AggregateException aggEx3)
                {
                    throw new AggregateException([.. aggEx1.InnerExceptions, .. aggEx3.InnerExceptions]);
                }

                throw new AggregateException([.. aggEx1.InnerExceptions, ex]);
            }

            if (ex is AggregateException aggEx2)
            {
                throw new AggregateException([wait.Exception!, .. aggEx2.InnerExceptions]);
            }

            throw new AggregateException(wait.Exception!, ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T Rent<T>(int minimumLength, out T[] taskArray)
    {
        taskArray = ArrayPool<T>.Shared.Rent(minimumLength);
        return ref MemoryMarshal.GetArrayDataReference(taskArray);
    }

    private static ref T Grow<T>(int minimumLength, scoped ref T[] array)
    {
        int capacity = array.Length;

        do
        {
            capacity += capacity >> 1;
        }
        while (capacity < minimumLength);

        T[] oldArray = array;
        ref T item = ref Rent(capacity, out array);
        Array.Copy(oldArray, array, oldArray.Length);

        ArrayPool<T>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        return ref item;
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw(Exception ex)
    {
        throw ex;
    }
}