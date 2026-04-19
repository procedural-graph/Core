using System.Collections.Immutable;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

internal abstract class AsyncEvent<TArgs> : Disposable, IAsyncEvent<TArgs>
{
    protected abstract ILogger Logger { get; }

    internal ImmutableArray<AsyncEventListener<TArgs>> listeners = [];

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _syncRoot = new();
#else
    private readonly object _syncRoot = new();
#endif

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
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
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
                AsyncEventListener<TArgs> newSubscription = new(handler, channel, Logger, scheduler);
                newSubscription.Start();
                newSubscription.StoppingToken.Register(Unsubscribe, newSubscription);

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

    protected override void OnDisposing()
    {
        ImmutableArray<AsyncEventListener<TArgs>> listeners = ImmutableInterlocked.InterlockedExchange(ref this.listeners, []);
        foreach (AsyncEventListener<TArgs> listener in listeners)
        {
            listener.Dispose();
        }
    }

    private void Unsubscribe(object? state)
    {
        lock (_syncRoot)
        {
            listeners = listeners.Remove((AsyncEventListener<TArgs>)state!);
        }
    }
}
