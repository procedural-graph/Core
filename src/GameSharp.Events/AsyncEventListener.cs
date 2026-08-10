using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Events;

public abstract partial class AsyncEventListener<TSender, TEventArgs> : AsyncLifecycle, IEquatable<AsyncEventListener<TSender, TEventArgs>>
{
    protected internal AsyncEventHandler<TSender, TEventArgs> EventHandler { get; }

    internal AsyncEventListener(AsyncEventHandler<TSender, TEventArgs> eventHandler)
    {
        EventHandler = eventHandler;
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] AsyncEventListener<TSender, TEventArgs>? other)
    {
        return ReferenceEquals(EventHandler, other?.EventHandler);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is AsyncEventListener<TSender, TEventArgs> other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => EventHandler.GetHashCode();

    public virtual bool TryGetLastError([NotNullWhen(true)] out Exception? exception)
    {
        exception = null;
        return false;
    }

    internal void Unsubscribe(object? state)
    {
        ((ICollection<AsyncEventListener<TSender, TEventArgs>>)state!).Remove(this);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "An unhandled exception occurred while invoking an asynchronous event handler.")]
    protected static partial void EventHandlerThrewAnException(ILogger logger, Exception exception);
}
