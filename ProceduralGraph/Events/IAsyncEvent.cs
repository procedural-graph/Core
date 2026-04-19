using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Defines an interface for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <typeparam name="TArgs">The type of the event arguments.</typeparam>
public interface IAsyncEvent<TArgs>
{
    /// <summary>
    /// Adds the specified callback to the list of subscribers.
    /// </summary>
    /// <param name="handler">
    /// The callback to be added to the list of subscribers.
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="scheduler">The <see cref="TaskScheduler"/> on which the callback will be executed.</param>
    /// <returns>A subscription object that can be used to unsubscribe the callback.</returns>
    AsyncEventSubscription<TArgs> Subscribe(AsyncEventHandler<TArgs> handler, TaskScheduler scheduler);
}
