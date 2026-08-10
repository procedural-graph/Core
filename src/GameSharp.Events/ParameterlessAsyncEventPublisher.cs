using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace GameSharp.Events;

public readonly struct ParameterlessAsyncEventPublisher<TSender, TEventArgs> : IDisposable, IAsyncDisposable
{
    private readonly AsyncEventBus<TSender, TEventArgs> _eventBus;
    private readonly ILogger _logger;

    public AsyncEvent<TSender, TEventArgs> Event { get; }

    internal ParameterlessAsyncEventPublisher(AsyncEventBus<TSender, TEventArgs> eventBus, ILogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
        Event = new AsyncEvent<TSender, TEventArgs>(eventBus);
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish(TSender sender, CancellationToken cancellationToken = default)
    {
        foreach (AsyncEventListener<TSender, TEventArgs> listener in _eventBus)
        {
#if DEBUG
            IAsyncEventListener<TSender> typedListener = (IAsyncEventListener<TSender>)listener;
#else
            IAsyncEventListener<TSender> typedListener = Unsafe.As<IAsyncEventListener<TSender>>(listener);
#endif
            typedListener.Invoke(sender, cancellationToken);
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous publish operation.</returns>
    /// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync(TSender sender, CancellationToken cancellationToken = default)
    {
        return AsyncEventPublisher.InvokeAsync(_eventBus, sender, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        AsyncEventPublisher.Dispose(_eventBus);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask DisposeAsync()
    {
        return AsyncEventPublisher.DisposeAsync(_eventBus);
    }
}
