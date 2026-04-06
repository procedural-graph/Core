using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

public sealed class AsyncEventPublisher<TArgs>(AsyncEventSubscription<TArgs> subscription, AsyncEventHandler<TArgs> handler, ChannelWriter<TArgs> writer) : IDisposable
{
    private readonly ChannelWriter<TArgs> _writer = writer;
    private bool _disposed;

    public AsyncEventSubscription<TArgs> Subscription { get; } = subscription;

    public AsyncEventHandler<TArgs> Event { get; } = handler;

    public ValueTask InvokeAsync(TArgs args, CancellationToken cancellationToken)
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);
        return _writer.WriteAsync(args, cancellationToken);
    }

    public bool TryInvoke(TArgs args) => !_disposed && _writer.TryWrite(args);

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _writer.Complete();
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
    }
}
