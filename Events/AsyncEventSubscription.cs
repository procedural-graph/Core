using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a subscription to an asynchronous event.
/// </summary>
/// <inheritdoc cref="AsyncEventHandler{TArgs}"/>
public sealed class AsyncEventSubscription<TArgs> : IDisposable, IAsyncDisposable
{
    private readonly ChannelReader<TArgs> _reader;
    private readonly AsyncEventHandler<TArgs> _handler;
    private readonly ILogger _logger;
    private CancellationTokenSource? _cts;
    private Task _invocationHandler;
    private volatile bool _disposed;

    internal AsyncEventSubscription(ChannelReader<TArgs> reader, AsyncEventHandler<TArgs> handler, ILogger logger)
    {
        _invocationHandler = Task.CompletedTask;
        _reader = reader;
        _handler = handler;
        _logger = logger;
    }

    internal CancellationToken Start()
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);

        CancellationTokenSource? newCts = new(), oldCts = Interlocked.CompareExchange(ref _cts, newCts, null);

        if (oldCts is { })
        {
            newCts.Dispose();
            throw new InvalidOperationException("Subscription has already been started.");
        }

        CancellationToken cancellationToken = newCts.Token;
        _invocationHandler = HandleInvocationsAsync(cancellationToken);

        return cancellationToken;
    }

    private async Task HandleInvocationsAsync(CancellationToken cancellationToken)
    {
        IAsyncEnumerable<TArgs> events = _reader.ReadAllAsync(cancellationToken);
        await foreach (TArgs args in events.ConfigureAwait(false))
        {
            try
            {
                ValueTask process = _handler.Invoke(args, cancellationToken);
                await process.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogException(ex);
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing && _cts is { })
        {
            try
            {
                _cts.Cancel();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogException(ex);
            }
            finally
            {
                _cts.Dispose();
            }
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Dispose(disposing: true);

        try
        {
            await _invocationHandler.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogException(ex);
        }
    }
}
