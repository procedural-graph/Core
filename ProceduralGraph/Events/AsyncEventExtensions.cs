using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Provides extension methods for subscribing to asynchronous events.
/// </summary>
public static class AsyncEventExtensions
{
    /// <param name="asyncEvent">The event to subscribe to.</param>
    /// <inheritdoc cref="IAsyncEvent{TArgs}.Subscribe(AsyncEventHandler{TArgs}, TaskScheduler)"/>
    /// <param name="handler"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AsyncEventSubscription<TArgs> Subscribe<TArgs>(this IAsyncEvent<TArgs> asyncEvent, AsyncEventHandler<TArgs> handler)
    {
        return asyncEvent.Subscribe(handler, TaskScheduler.Default);
    }
}
