using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Provides factory methods for creating asynchronous event publishers.
/// </summary>
public static class AsyncEventPublisher
{
    private static readonly BoundedChannelOptions _conflatingEventChannelOptions = new(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    };

    /// <remarks>
    /// Previous events not yet processed by subscribers will be overwritten by new events, 
    /// ensuring that subscribers always receive the most recent event.
    /// </remarks>
    /// <inheritdoc cref="Create{TArgs}(BoundedChannelOptions, ILogger, Action{TArgs}?)"/>
    public static AsyncEventPublisher<TArgs> CreateConflating<TArgs>(ILogger logger)
    {
        return Create<TArgs>(_conflatingEventChannelOptions, logger);
    }

    /// <inheritdoc cref="Create{TArgs}(BoundedChannelOptions, ILogger, Action{TArgs}?)"/>
    public static AsyncEventPublisher<TArgs> Create<TArgs>(ILogger logger)
    {
        ThrowHelpers.ThrowIfNull(logger, nameof(logger));
        DefaultAsyncEvent<TArgs> asyncEvent = new(logger);
        return new AsyncEventPublisher<TArgs>(asyncEvent);
    }

    /// <inheritdoc cref="Create{TArgs}(BoundedChannelOptions, ILogger, Action{TArgs}?)"/>
    public static AsyncEventPublisher<TArgs> Create<TArgs>(UnboundedChannelOptions channelOptions, ILogger logger)
    {
        ThrowHelpers.ThrowIfNull(logger, nameof(logger));
        ThrowHelpers.ThrowIfNull(channelOptions, nameof(channelOptions));
        UnboundedAsyncEvent<TArgs> asyncEvent = new(logger, channelOptions);
        return new AsyncEventPublisher<TArgs>(asyncEvent);
    }

    /// <inheritdoc cref="Create{TArgs}(BoundedChannelOptions, ILogger, Action{TArgs}?)"/>
    public static AsyncEventPublisher<TArgs> Create<TArgs>(BoundedChannelOptions channelOptions, ILogger logger)
    {
        ThrowHelpers.ThrowIfNull(logger, nameof(logger));
        ThrowHelpers.ThrowIfNull(channelOptions, nameof(channelOptions));
        BoundedAsyncEvent<TArgs> asyncEvent = new(logger, channelOptions);
        return new AsyncEventPublisher<TArgs>(asyncEvent);
    }

    /// <summary>
    /// Creates a new asynchronous event publisher.
    /// </summary>
    /// <param name="channelOptions">
    /// The options used to configure the channel for event publishing. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="logger">
    /// The logger used to record event processing information. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="eventDropped">
    /// An optional callback invoked when an event is dropped due to channel constraints. 
    /// May be <see langword="null"/>.
    /// </param>
    /// <returns>A new instance of <see cref="AsyncEventPublisher{TArgs}"/>.</returns>
    /// <inheritdoc cref="AsyncEvent{TArgs}"/>
    public static AsyncEventPublisher<TArgs> Create<TArgs>(BoundedChannelOptions channelOptions, ILogger logger, Action<TArgs>? eventDropped)
    {
        ThrowHelpers.ThrowIfNull(logger, nameof(logger));
        ThrowHelpers.ThrowIfNull(channelOptions, nameof(channelOptions));
        BoundedAsyncEvent<TArgs> asyncEvent = new(logger, channelOptions, eventDropped);
        return new AsyncEventPublisher<TArgs>(asyncEvent);
    }
}

/// <summary>
/// Provides a mechanism for publishing asynchronous events to multiple subscribers using the specified event argument
/// type.
/// </summary>
/// <inheritdoc cref="AsyncEvent{TArgs}"/>
public readonly struct AsyncEventPublisher<TArgs>
{
    /// <summary>
    /// Gets the asynchronous event to which subscribers can subscribe to receive notifications when the event is published.
    /// </summary>
    public AsyncEvent<TArgs> Event { get; }

    internal AsyncEventPublisher(AsyncEvent<TArgs> asyncEvent)
    {
        Event = asyncEvent;
    }

    /// <summary>
    /// Publishes an event with the specified arguments to all subscribers.
    /// </summary>
    /// <param name="args">The event data to pass to each event handler.</param>
    public void Publish(TArgs args)
    {
        foreach (AsyncEventListener<TArgs> listener in Event.listeners)
        {
            listener.TryInvoke(args);
        }
    }

    /// <param name="cancellationToken">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous publish operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <inheritdoc cref="Publish(TArgs)"/>
    /// <param name="args"/>
    public async ValueTask PublishAsync(TArgs args, CancellationToken cancellationToken = default)
    {
        ImmutableArray<AsyncEventListener<TArgs>> listeners = Event.listeners;
        Task[] tasks = ArrayPool<Task>.Shared.Rent(listeners.Length);
        AggregateException? aggregateException = null;
        try
        {
            int taskIndex = 0;
            foreach (AsyncEventListener<TArgs> listener in listeners)
            {
                ValueTask invokeTask = listener.InvokeAsync(args, cancellationToken);

                if (!invokeTask.IsCompleted)
                {
                    tasks[taskIndex++] = invokeTask.AsTask();
                    continue;
                }

                try
                {
                    invokeTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    aggregateException = AppendException(aggregateException, ex);
                    if (ex is OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            if (taskIndex <= 0)
            {
                return;
            }

#if NET9_0_OR_GREATER
            Task wait = Task.WhenAll(tasks.AsSpan(0, taskIndex));
#else
            Task wait = Task.WhenAll(System.Linq.Enumerable.Take(tasks, taskIndex));
#endif
            await wait.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            aggregateException = AppendException(aggregateException, ex);
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(tasks, clearArray: true);
        }

        if (aggregateException is { })
        {
            throw aggregateException;
        }
    }

    private static AggregateException AppendException(AggregateException? aggregate, Exception exception)
    {
        IEnumerable<Exception> aggregateExceptions = EnumerateExceptions(aggregate);
        IEnumerable<Exception> additionalExceptions = EnumerateExceptions(exception);
        return new AggregateException(aggregateExceptions.Concat(additionalExceptions));
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception? exception)
    {
        if (exception is null)
        {
            yield break;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (Exception inner in aggregateException.InnerExceptions)
            {
                yield return inner;
            }
        }
        else
        {
            yield return exception;
        }
    }
}
