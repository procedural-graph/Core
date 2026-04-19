using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a listener for asynchronous events with arguments of type <typeparamref name="TArgs"/>.
/// </summary>
/// <inheritdoc cref="AsyncEvent{TArgs}"/>
public sealed class AsyncEventListener<TArgs> : AsyncLifecycle, IEquatable<AsyncEventListener<TArgs>>
{
    private readonly ILogger _logger;
    private readonly Channel<TArgs> _channel;
    private readonly TaskScheduler _scheduler;

    internal AsyncEventHandler<TArgs> Event { get; }

    internal AsyncEventListener(AsyncEventHandler<TArgs> handler, Channel<TArgs> channel, ILogger logger, TaskScheduler scheduler)
    {
        _channel = channel;
        _logger = logger;
        _scheduler = scheduler;
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

    /// <inheritdoc/>
    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        Task<Task> task = Task.Factory.StartNew(HandleInvocationsAsync, cancellationToken, TaskCreationOptions.DenyChildAttach, _scheduler);
        return task.Unwrap();
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
        await foreach (TArgs args in _channel.Reader.ReadAllAsync(StoppingToken))
        {
            try
            {
                await Event.Invoke(args, StoppingToken);
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
        try
        {
            base.OnDisposing();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions during disposal
        }
        catch (Exception ex)
        {
            _logger.LogException(ex);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions during disposal
        }
        catch (Exception ex)
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
