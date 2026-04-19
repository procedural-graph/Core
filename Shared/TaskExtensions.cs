using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ProceduralGraph;

internal static class TaskExtensions
{
    public static async ValueTask CompleteAllAsync<T>(this T processes) where T : struct, IEnumerator<ValueTask>
    {
        Task[]? tasks = RentedArray.Acquire<Task>();
        AggregateException? aggregateException = null;
        int count = 0;
        while (processes.MoveNext())
        {
            try
            {
                if (processes.Current.TryCompleteSynchronously(out Task? task))
                {
                    continue;
                }

                int index = count++;
                RentedArray.Grow(ref tasks, count);
                tasks[index] = task;
            }
            catch (Exception ex)
            {
                aggregateException = AppendException(aggregateException, ex);
            }
        }

        if (count == 0)
        {
            return;
        }

        try
        {
#if NET9_0_OR_GREATER
            await Task.WhenAll(tasks.AsSpan(0, count));
#else
            await Task.WhenAll(Enumerable.Take(tasks, count));
#endif
        }
        catch (Exception ex)
        {
            aggregateException = AppendException(aggregateException, ex);
        }
        finally
        {
            RentedArray.Return(ref tasks);
        }

        if (aggregateException is { })
        {
            throw aggregateException;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCompleteSynchronously(this ValueTask valueTask, [NotNullWhen(false)] out Task? result)
    {
        if (valueTask.IsCompleted)
        {
            result = null;
            valueTask.GetAwaiter().GetResult();
            return true;
        }

        result = valueTask.AsTask();
        return false;
    }

    private static AggregateException AppendException(AggregateException? aggregate, Exception exception)
    {
        IEnumerable<Exception> aggregateExceptions = EnumerateExceptions(aggregate);
        IEnumerable<Exception> additionalExceptions = EnumerateExceptions(exception);
        return new AggregateException(aggregateExceptions.Concat(additionalExceptions));
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception? exception)
    {
        if (exception is null)
        {
            yield break;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (Exception inner in aggregateException.InnerExceptions)
            {
                yield return inner;
            }
        }
        else
        {
            yield return exception;
        }
    }
}