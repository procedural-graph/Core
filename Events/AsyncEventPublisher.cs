using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Provides a publisher for asynchronously invoking event handlers with arguments of the specified type.
/// </summary>
/// <inheritdoc cref="AsyncEventHandler{TArgs}"/>
public readonly struct AsyncEventPublisher<TArgs> : IDisposable
{
    private readonly ChannelWriter<TArgs> _writer;

    /// <summary>
    /// Gets the subscription associated with this publisher.
    /// </summary>
    public AsyncEventSubscription<TArgs> Subscription { get; }

    internal AsyncEventPublisher(AsyncEventSubscription<TArgs> subscription, ChannelWriter<TArgs> writer)
    {
        _writer = writer;
        Subscription = subscription;
    }

    /// <summary>
    /// Asynchronously enqueues the specified arguments for processing, waiting until space is available or the
    /// operation is canceled.
    /// </summary>
    /// <param name="args">The arguments to enqueue for processing.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the enqueue operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous enqueue operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the underlying writer has been completed.</exception>
    public async ValueTask InvokeAsync(TArgs args, CancellationToken cancellationToken)
    {
        while (await _writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_writer.TryWrite(args))
            {
                return;
            }
        }

        ThrowHelpers.ThrowObjectDisposedException(this);
    }

    /// <summary>
    /// Attempts to invoke the operation with the specified arguments without blocking.
    /// </summary>
    /// <param name="args">The arguments to enqueue for processing.</param>
    /// <returns><see langword="true"/> if the operation was successfully invoked; otherwise, <see langword="false"/>.</returns>
    public bool TryInvoke(TArgs args) => _writer.TryWrite(args);

    /// <inheritdoc/>
    public void Dispose() => _writer.TryComplete();
}
