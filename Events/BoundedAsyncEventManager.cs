using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents an asynchronous event manager that uses a bounded channel to manage event handlers and publish events to subscribers.
/// </summary>
/// <inheritdoc/>
/// <remarks>
/// Initializes a new instance of the <see cref="BoundedAsyncEventManager{TArgs}"/> class with the specified logger.
/// </remarks>
/// <param name="logger">
/// The logger to use for recording diagnostic and operational messages. 
/// Cannot be <see langword="null"/>.
/// </param>
/// <param name="channelOptions">The options to configure the bounded channel. Cannot be <see langword="null"/>.</param>
/// <param name="eventDropped">The action to invoke when an event is dropped. Can be <see langword="null"/>.</param>
/// <exception cref="ArgumentNullException">Thrown if logger is <see langword="null"/>.</exception>
public sealed class BoundedAsyncEventManager<TArgs>(ILogger logger, BoundedChannelOptions channelOptions, Action<TArgs>? eventDropped = null) : 
    AsyncEventManager<TArgs>()
{
    private readonly BoundedChannelOptions _channelOptions = channelOptions ?? throw new ArgumentNullException(nameof(channelOptions));
    private readonly Action<TArgs>? _eventDropped = eventDropped;

    /// <inheritdoc/>
    protected override ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    protected override Channel<TArgs> CreateChannel() => Channel.CreateBounded(_channelOptions, _eventDropped);

    /// <summary>
    /// Publishes an event with the specified arguments to all subscribers asynchronously.
    /// </summary>
    /// <param name="args">The event data to pass to each event handler.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the publish operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous publish operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    /// <inheritdoc cref="AsyncEventManager{TArgs}.Subscribe(AsyncEventHandler{TArgs})"/>
    public async ValueTask PublishAsync(TArgs args, CancellationToken cancellationToken = default)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        Collection items = Publishers;
        Task[] tasks = ArrayPool<Task>.Shared.Rent(items.Count);
        AggregateException? aggregateException = null;
        try
        {
            int taskIndex = 0;
            foreach (var item in items)
            {
                ValueTask invokeTask = item.InvokeAsync(args, cancellationToken);

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

    /// <summary>
    /// Invokes all registered event handlers synchronously with the specified event arguments.
    /// </summary>
    /// <param name="args">The event data to pass to each event handler.</param>
    /// <exception cref="InvalidOperationException">Thrown if any event handler cannot be completed synchronously.</exception>
    /// <inheritdoc cref="AsyncEventManager{TArgs}.Subscribe(AsyncEventHandler{TArgs})"/>
    public void Publish(TArgs args)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        foreach (AsyncEventPublisher<TArgs> subscription in Publishers)
        {
            ThrowHelpers.ThrowIf(!subscription.TryInvoke(args), "Event handler could not be completed synchronously.");
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
