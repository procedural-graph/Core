using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a manager for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <inheritdoc cref="AsyncEventHandler{TArgs}"/>
public abstract class AsyncEventManager<TArgs>
{
    private sealed record PublisherSource(AsyncEventPublisher<TArgs>?[] Publishers, int Low = 0, int High = 0);

    private const int MinArrayCapacity = 4;

    /// <summary>
    /// Gets the logger instance used to record diagnostic and operational messages.
    /// </summary>
    protected abstract ILogger Logger { get; }

    private readonly ConcurrentDictionary<AsyncEventHandler<TArgs>, int> _handlers;
    private PublisherSource _publisherSource;

    /// <summary>
    /// Gets a <see cref="Span{T}"/> of the current publishers for the asynchronous events.
    /// </summary>
    protected Span<AsyncEventPublisher<TArgs>?> Publishers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            (AsyncEventPublisher<TArgs>?[] publishers, _, int high) = Volatile.Read(ref _publisherSource); 
            return publishers.AsSpan(0, high);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncEventManager{TArgs}"/> class with the specified logger.
    /// </summary>
    public AsyncEventManager()
    {
        AsyncEventPublisher<TArgs>?[] array = new AsyncEventPublisher<TArgs>?[MinArrayCapacity];
        _publisherSource = new PublisherSource(array);
        _handlers = [];
    }

    /// <summary>
    /// Adds the specified callback to the list of subscribers.
    /// </summary>
    /// <param name="handler">
    /// The callback to be added to the list of subscribers.
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>A subscription object that can be used to unsubscribe the callback.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsyncEventSubscription<TArgs> Subscribe(AsyncEventHandler<TArgs> handler)
    {
        AsyncEventPublisher<TArgs>? publisher;
        do
        {
            if (TryGetPublisher(handler, out publisher))
            {
                return publisher.Subscription;
            }
        }
        while (!TryAddPublisher(handler, out publisher));
        return publisher.Subscription;
    }

    /// <summary>
    /// Creates a new channel for processing messages of type <typeparamref name="TArgs"/>.
    /// </summary>
    /// <returns>A channel instance that can be used to send and receive messages of type <typeparamref name="TArgs"/>.</returns>
    protected abstract Channel<TArgs> CreateChannel();

    private bool TryGetPublisher(AsyncEventHandler<TArgs> handler, [NotNullWhen(true)] out AsyncEventPublisher<TArgs>? publisher)
    {
        do
        {
            int index;
            PublisherSource currentValue = Volatile.Read(ref _publisherSource), oldValue;
            do
            {
                if (!_handlers.TryGetValue(handler, out index))
                {
                    publisher = default;
                    return false;
                }

                (currentValue, oldValue) = (Volatile.Read(ref _publisherSource), currentValue);
            }
            while (!ReferenceEquals(currentValue, oldValue));
            publisher = Volatile.Read(ref currentValue.Publishers[index]);
        }
        while (publisher is null);
        return true;
    }

    private bool TryAddPublisher(AsyncEventHandler<TArgs> handler, [NotNullWhen(true)] out AsyncEventPublisher<TArgs>? publisher)
    {
        publisher = null;
        AsyncEventSubscription<TArgs>? subscriber = null;
        Channel<TArgs>? channel = null;

        PublisherSource currentValue = Volatile.Read(ref _publisherSource), oldValue, newValue;
        do
        {
            oldValue = currentValue;

            if (_handlers.ContainsKey(handler))
            {
                publisher?.Dispose();
                publisher = null;
                return false;
            }

            channel ??= CreateChannel();
            subscriber ??= new AsyncEventSubscription<TArgs>(channel.Reader, handler, Logger);
            publisher ??= new AsyncEventPublisher<TArgs>(subscriber, handler, channel.Writer);

            (AsyncEventPublisher<TArgs>?[] publishers, int low, int high) = currentValue;

            for (; low < high; low++)
            {
                if (Interlocked.CompareExchange(ref publishers[low], publisher, null) is null)
                {
                    _handlers[handler] = low++;
                    goto CompareAndSwap;
                }
            }

            publishers = Grow(publishers, ++high);
            _handlers[handler] = low;
            publishers[low] = publisher;
            low = high;

        CompareAndSwap:
            newValue = new(publishers, low, high);
            currentValue = Interlocked.CompareExchange(ref _publisherSource, newValue, oldValue);
        }
        while (!ReferenceEquals(currentValue, oldValue));

        CancellationToken cancellationToken = subscriber.Start();
        cancellationToken.Register(static s => ((AsyncEventSubscription<TArgs>)s!).Dispose(), subscriber);
        cancellationToken.Register(Unsubscribe, handler);

        return true;
    }

    private void Unsubscribe(object? state)
    {
        AsyncEventHandler<TArgs> handler = (AsyncEventHandler<TArgs>)state!;

        if (!TryRemovePublisher(handler, out int index))
        {
            return;
        }

        PublisherSource currentSource = Volatile.Read(ref _publisherSource), oldSource;
        do
        {
            if (index > currentSource.Low)
            {
                return;
            }

            (currentSource, oldSource) = (currentSource with { Low = index }, currentSource);
            currentSource = Interlocked.CompareExchange(ref _publisherSource, currentSource, oldSource);
        }
        while (!ReferenceEquals(currentSource, oldSource));
    }

    private bool TryRemovePublisher(AsyncEventHandler<TArgs> handler, out int index)
    {
        AsyncEventPublisher<TArgs>? currentPublisher = null, oldPublisher;
        do
        {
            PublisherSource currentSource = Volatile.Read(ref _publisherSource), oldSource;
            do
            {
                if (!_handlers.TryRemove(handler, out index))
                {
                    return false;
                }

                (currentSource, oldSource) = (Volatile.Read(ref _publisherSource), currentSource);
            }
            while (!ReferenceEquals(currentSource, oldSource));

            ref AsyncEventPublisher<TArgs>? publisherRef = ref currentSource.Publishers[index];
            oldPublisher = Volatile.Read(ref publisherRef);
            if (ReferenceEquals(oldPublisher?.Event, handler))
            {
                currentPublisher = Interlocked.CompareExchange(ref publisherRef, null, oldPublisher);
            }
        }
        while (!ReferenceEquals(currentPublisher, oldPublisher));
        return true;
    }

    private static T[] Grow<T>(T[] array, int newLength)
    {
        int currentLength = array.Length;
        if (newLength <= array.Length)
        {
            return array;
        }

        int optimumLength = currentLength;
        do
        {
            optimumLength *= 2;
        }
        while (optimumLength < newLength);

        T[] newArray = new T[optimumLength];
        Array.Copy(array, 0, newArray, 0, currentLength);
        return newArray;
    }
}
