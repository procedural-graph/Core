using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a listener for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <inheritdoc cref="AsyncEvent{TArgs}"/>
public sealed class AsyncEventListener<TArgs> : Disposable, IAsyncDisposable, IEquatable<AsyncEventListener<TArgs>>
{
    private readonly ILogger _logger;
    private readonly Channel<TArgs> _channel;
    private CancellationTokenSource? _cts;
    private Task _invocationHandler;

    internal AsyncEventHandler<TArgs> Event { get; }

    internal AsyncEventListener(AsyncEventHandler<TArgs> handler, Channel<TArgs> channel, ILogger logger)
    {
        _channel = channel;
        _invocationHandler = Task.CompletedTask;
        _logger = logger;
        Event = handler;
    }

    /// <inheritdoc/>
    public bool Equals(AsyncEventListener<TArgs>? other)
    {
        return ReferenceEquals(Event, other?.Event);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is AsyncEventListener<TArgs> other && Equals(other);
    }

    /// <inheritdoc/>
    override public int GetHashCode() => Event.GetHashCode();

    internal CancellationToken Start(TaskScheduler scheduler)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);

        CancellationTokenSource? newCts = new(), oldCts = Interlocked.CompareExchange(ref _cts, newCts, null);

        if (oldCts is { })
        {
            newCts.Dispose();
            throw new InvalidOperationException("Subscription has already been started.");
        }

        CancellationToken cancellationToken = newCts.Token;

        Task<Task> task = Task.Factory.StartNew(HandleInvocationsAsync, cancellationToken, TaskCreationOptions.DenyChildAttach, scheduler);
        _invocationHandler = task.Unwrap();

        return cancellationToken;
    }

    /// <summary>
    /// Asynchronously enqueues the specified arguments for processing, waiting until space is available or the
    /// operation is canceled.
    /// </summary>
    /// <param name="args">The arguments to enqueue for processing.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the enqueue operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous enqueue operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the underlying writer has been completed.</exception>
    public ValueTask InvokeAsync(TArgs args, CancellationToken cancellationToken)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return _channel.Writer.WriteAsync(args, cancellationToken);
    }

    /// <summary>
    /// Attempts to invoke the operation with the specified arguments without blocking.
    /// </summary>
    /// <param name="args">The arguments to enqueue for processing.</param>
    /// <returns><see langword="true"/> if the operation was successfully invoked; otherwise, <see langword="false"/>.</returns>
    public bool TryInvoke(TArgs args) => _channel.Writer.TryWrite(args);

    private async Task HandleInvocationsAsync()
    {
        CancellationToken cancellationToken = _cts!.Token;
        await foreach (TArgs args in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await Event.Invoke(args, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogException(ex);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        _channel.Writer.TryComplete();

        if (_cts is null)
        {
            return;
        }

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

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Dispose();

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
    public static bool operator ==(AsyncEventListener<TArgs>? left, AsyncEventListener<TArgs>? right) => Equals(left, right);

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns><see langword="true"/> if left is not equal to right; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(AsyncEventListener<TArgs>? left, AsyncEventListener<TArgs>? right) => !Equals(left, right);
}
