using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

internal static partial class TaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredCancelableAsyncEnumerable<Task<T>> WhenEach<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
    {
        return Task.WhenEach(tasks).WithCancellation(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncEnumerable<Task<T>> WhenEach<T>(this IEnumerable<Task<T>> tasks)
    {
        return Task.WhenEach(tasks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredCancelableAsyncEnumerable<Task> WhenEach(this IEnumerable<Task> tasks, CancellationToken cancellationToken = default)
    {
        return Task.WhenEach(tasks).WithCancellation(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAsyncEnumerable<Task> WhenEach(this IEnumerable<Task> tasks)
    {
        return Task.WhenEach(tasks);
    }
}