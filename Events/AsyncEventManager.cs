using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a manager for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <inheritdoc cref="AsyncEventHandler{TArgs}"/>
public abstract class AsyncEventManager<TArgs> : Disposable
{
    /// <inheritdoc cref="IEnumerator{T}"/>
    protected ref struct Enumerator
    {
        private readonly ImmutableArray<AsyncEventSubscription<TArgs>> _subscriptions;
        private int _index;

        /// <inheritdoc cref="ICollection.Count"/>
        public readonly int Count => _subscriptions.Length;

        internal Enumerator(ImmutableArray<AsyncEventSubscription<TArgs>> subscriptions)
        {
            _subscriptions = subscriptions;
            _index = -1;
            Current = default!;
        }

        /// <inheritdoc cref="IEnumerator.Current"/>
        public AsyncEventPublisher<TArgs> Current { get; private set; }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            if (++_index >= _subscriptions.Length)
            {
                return false;
            }

            Current = _subscriptions[_index].Publisher;

            return true;
        }
    }

    /// <summary>
    /// Represents a read-only collection of asynchronous event publishers.
    /// </summary>
    protected readonly ref struct Collection : IReadOnlyCollection<AsyncEventPublisher<TArgs>>
    {
        private readonly ImmutableArray<AsyncEventSubscription<TArgs>> _subscriptions;

        internal Collection(ImmutableArray<AsyncEventSubscription<TArgs>> subscriptions)
        {
            _subscriptions = subscriptions;
        }

        /// <inheritdoc cref="ICollection.Count"/>
        public readonly int Count => _subscriptions.Length;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_subscriptions);
        }

        private IEnumerator<AsyncEventPublisher<TArgs>> GetEnumeratorAlloc()
        {
            IEnumerable<AsyncEventPublisher<TArgs>> publishers = System.Linq.Enumerable.Select(_subscriptions, static s => s.Publisher);
            return publishers.GetEnumerator();
        }

        IEnumerator<AsyncEventPublisher<TArgs>> IEnumerable<AsyncEventPublisher<TArgs>>.GetEnumerator()
        {
            return GetEnumeratorAlloc();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorAlloc();
        }
    }

    /// <summary>
    /// Gets the logger instance used to record diagnostic and operational messages.
    /// </summary>
    protected abstract ILogger Logger { get; }

    private ImmutableArray<AsyncEventSubscription<TArgs>> _subscriptions = [];

    /// <summary>
    /// Gets a collection of the current asynchronous event publishers subscribed to this event manager.
    /// </summary>
    protected Collection Publishers => new(_subscriptions);

#if NET9_0_OR_GREATER
    private readonly Lock _syncRoot = new();
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
    /// <returns>A subscription object that can be used to unsubscribe the callback.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the event manager has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsyncEventSubscription<TArgs> Subscribe(AsyncEventHandler<TArgs> handler)
    {
        ImmutableArray<AsyncEventSubscription<TArgs>> currentSubscriptions = _subscriptions, oldSubscriptions;
        while (true)
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);

            for (int i = 0; i < currentSubscriptions.Length; i++)
            {
                AsyncEventSubscription<TArgs> subscription = currentSubscriptions[i];
                if (ReferenceEquals(subscription.Event, handler))
                {
                    return subscription;
                }
            }

            lock (_syncRoot)
            {
                (oldSubscriptions, currentSubscriptions) = (currentSubscriptions, _subscriptions);

                if (currentSubscriptions != oldSubscriptions)
                {
                    continue;
                }

                AsyncEventSubscription<TArgs> newSubscription = new(handler, Logger);

                Channel<TArgs> channel = CreateChannel();
                CancellationToken cancellationToken = newSubscription.Start(channel);

                cancellationToken.Register(static s => ((ChannelWriter<TArgs>)s!).TryComplete(), channel.Writer);
                cancellationToken.Register(Unsubscribe, newSubscription);

                _subscriptions = currentSubscriptions.Add(newSubscription);

                return newSubscription;
            }
        }
    }

    /// <summary>
    /// Creates a new channel for processing messages of type <typeparamref name="TArgs"/>.
    /// </summary>
    /// <returns>A channel instance that can be used to send and receive messages of type <typeparamref name="TArgs"/>.</returns>
    protected abstract Channel<TArgs> CreateChannel();

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        ImmutableArray<AsyncEventSubscription<TArgs>> subscriptions;

        lock (_syncRoot)
        {
            subscriptions = _subscriptions;
            _subscriptions = [];
        }

        foreach (AsyncEventSubscription<TArgs> subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }

    private void Unsubscribe(object? state)
    {
        if (Disposed) 
        {
            return;
        }

        lock (_syncRoot)
        {
            _subscriptions = _subscriptions.Remove((AsyncEventSubscription<TArgs>)state!);
        }
    }
}
