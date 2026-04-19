using System;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a subscription to an asynchronous event with arguments of type <typeparamref name="TArgs"/>. 
/// Provides a handle for managing the subscription's lifecycle, including disposal to unsubscribe from the event.
/// </summary>
/// <inheritdoc cref="AsyncEvent{TArgs}"/>
public readonly struct AsyncEventSubscription<TArgs> : IDisposable, IAsyncDisposable
{
    private readonly AsyncEventListener<TArgs> _listener;

    internal AsyncEventSubscription(AsyncEventListener<TArgs> listener)
    {
        _listener = listener;
    }

    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _listener.DisposeAsync();
}
