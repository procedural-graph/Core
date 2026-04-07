using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a manager for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <typeparam name="TArgs">The type of the event arguments.</typeparam>
public abstract class AsyncEvent<TArgs>
{
    /// <summary>
    /// Gets the logger instance used to record diagnostic and operational messages.
    /// </summary>
    protected abstract ILogger Logger { get; }

    internal ImmutableArray<AsyncEventListener<TArgs>> listeners = [];
    /// <summary>
    /// Gets the current list of asynchronous event listeners subscribed to this event.
    /// </summary>
    protected ImmutableArray<AsyncEventListener<TArgs>> Listeners => listeners;

#if NET9_0_OR_GREATER
    private readonly Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif

    /// <inheritdoc cref="Subscribe(AsyncEventHandler{TArgs}, TaskScheduler)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsyncEventSubscription<TArgs> Subscribe(AsyncEventHandler<TArgs> handler)
    {
        return Subscribe(handler, TaskScheduler.Default);
    }

    /// <summary>
    /// Adds the specified callback to the list of subscribers.
    /// </summary>
    /// <param name="handler">
    /// The callback to be added to the list of subscribers.
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="scheduler">The <see cref="TaskScheduler"/> on which the callback will be executed.</param>
    /// <returns>A subscription object that can be used to unsubscribe the callback.</returns>
    public AsyncEventSubscription<TArgs> Subscribe(AsyncEventHandler<TArgs> handler, TaskScheduler scheduler)
    {
        ImmutableArray<AsyncEventListener<TArgs>> currentSubscriptions = listeners, oldSubscriptions;
        while (true)
        {
            for (int i = 0; i < currentSubscriptions.Length; i++)
            {
                AsyncEventListener<TArgs> subscription = currentSubscriptions[i];
                if (ReferenceEquals(subscription.Event, handler))
                {
                    return new AsyncEventSubscription<TArgs>(subscription);
                }
            }

            lock (_syncRoot)
            {
                (oldSubscriptions, currentSubscriptions) = (currentSubscriptions, listeners);

                if (currentSubscriptions != oldSubscriptions)
                {
                    continue;
                }

                Channel<TArgs> channel = CreateChannel();
                AsyncEventListener<TArgs> newSubscription = new(handler, channel, Logger);

                CancellationToken cancellationToken = newSubscription.Start(scheduler);
                cancellationToken.Register(Unsubscribe, newSubscription);

                listeners = currentSubscriptions.Add(newSubscription);

                return new AsyncEventSubscription<TArgs>(newSubscription);
            }
        }
    }

    /// <summary>
    /// Creates a new channel for processing messages of type <typeparamref name="TArgs"/>.
    /// </summary>
    /// <returns>A channel instance that can be used to send and receive messages of type <typeparamref name="TArgs"/>.</returns>
    protected abstract Channel<TArgs> CreateChannel();

    private void Unsubscribe(object? state)
    {
        lock (_syncRoot)
        {
            listeners = listeners.Remove((AsyncEventListener<TArgs>)state!);
        }
    }
}
