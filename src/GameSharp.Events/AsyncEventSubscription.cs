namespace GameSharp.Events;

/// <summary>
/// Represents a subscription to an asynchronous event. 
/// </summary>
/// <remarks>
/// Provides a handle for managing the subscription's lifecycle, including disposal to unsubscribe from the event.
/// </remarks>
/// <returns/>
/// <inheritdoc cref="AsyncEventHandler{TSender, TEventArgs}"/>
public readonly struct AsyncEventSubscription<TSender, TEventArgs> : IDisposable, IAsyncDisposable
{
    private readonly AsyncEventListener<TSender, TEventArgs> _listener;

    internal AsyncEventSubscription(AsyncEventListener<TSender, TEventArgs> listener)
    {
        _listener = listener;
    }

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _listener.DisposeAsync();
}
