using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Events;

/// <summary>
/// 
/// </summary>
/// <returns/>
/// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
public readonly struct AsyncEvent<TSender, TEventArgs> : IEquatable<AsyncEvent<TSender, TEventArgs>>
{
    private readonly AsyncEventBus<TSender, TEventArgs> _eventBus;

    internal AsyncEvent(AsyncEventBus<TSender, TEventArgs> eventBus)
    {
        _eventBus = eventBus;
    }

    public CancellationTokenRegistration Subscribe(AsyncEventHandler<TSender, TEventArgs> eventHandler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventHandler);
        AsyncEventListener<TSender, TEventArgs> listener = _eventBus.GetOrAdd(eventHandler);
        return cancellationToken.Register(static state => ((AsyncEventListener<TSender, TEventArgs>)state!).Dispose(), listener);
    }

    public AsyncEventSubscription<TSender, TEventArgs> Subscribe(AsyncEventHandler<TSender, TEventArgs> eventHandler)
    {
        ArgumentNullException.ThrowIfNull(eventHandler);
        AsyncEventListener<TSender, TEventArgs> listener = _eventBus.GetOrAdd(eventHandler);
        return new AsyncEventSubscription<TSender, TEventArgs>(listener);
    }

    /// <inheritdoc/>
    public bool Equals(AsyncEvent<TSender, TEventArgs> other)
    {
        return _eventBus.Equals(other._eventBus);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is AsyncEvent<TSender, TEventArgs> other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _eventBus.GetHashCode();
    }

    public static bool operator ==(AsyncEvent<TSender, TEventArgs> left, AsyncEvent<TSender, TEventArgs> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AsyncEvent<TSender, TEventArgs> left, AsyncEvent<TSender, TEventArgs> right)
    {
        return !(left == right);
    }
}
