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
public sealed class AsyncEventSubscription<TArgs> : IDisposable, IAsyncDisposable, IEquatable<AsyncEventSubscription<TArgs>>
{
    private readonly ILogger _logger;
    private CancellationTokenSource? _cts;
    private Task _invocationHandler;
    private volatile bool _disposed;

    internal AsyncEventHandler<TArgs> Event;

    internal AsyncEventPublisher<TArgs> Publisher { get; private set; }

    internal AsyncEventSubscription(AsyncEventHandler<TArgs> handler, ILogger logger)
    {
        _invocationHandler = Task.CompletedTask;
        _logger = logger;
        Event = handler;
    }

    /// <inheritdoc/>
    public bool Equals(AsyncEventSubscription<TArgs>? other)
    {
        return ReferenceEquals(Event, other?.Event);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is AsyncEventSubscription<TArgs> other && Equals(other);
    }

    /// <inheritdoc/>
    override public int GetHashCode() => Event.GetHashCode();

    internal CancellationToken Start(Channel<TArgs> channel)
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);

        CancellationTokenSource? newCts = new(), oldCts = Interlocked.CompareExchange(ref _cts, newCts, null);

        if (oldCts is { })
        {
            newCts.Dispose();
            throw new InvalidOperationException("Subscription has already been started.");
        }

        CancellationToken cancellationToken = newCts.Token;
        _invocationHandler = HandleInvocationsAsync(Event, channel.Reader, cancellationToken);

        Publisher = new AsyncEventPublisher<TArgs>(this, channel.Writer);

        return cancellationToken;
    }

    private async Task HandleInvocationsAsync(AsyncEventHandler<TArgs> handler, ChannelReader<TArgs> reader, CancellationToken cancellationToken)
    {
        IAsyncEnumerable<TArgs> events = reader.ReadAllAsync(cancellationToken);
        await foreach (TArgs args in events.ConfigureAwait(false))
        {
            try
            {
                ValueTask process = handler.Invoke(args, cancellationToken);
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

    /// <summary>
    /// Compares two values to determine equality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns><see langword="true"/> if left is equal to right; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(AsyncEventSubscription<TArgs>? left, AsyncEventSubscription<TArgs>? right) => Equals(left, right);

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns><see langword="true"/> if left is not equal to right; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(AsyncEventSubscription<TArgs>? left, AsyncEventSubscription<TArgs>? right) => !Equals(left, right);
}
