using System;
using System.Threading.Channels;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents an asynchronous event manager that uses a unbounded channel to manage event handlers and publish events to subscribers.
/// </summary>
/// <inheritdoc/>
/// <remarks>
/// Initializes a new instance of the <see cref="UnboundedAsyncEventManager{TArgs}"/> class with the specified logger.
/// </remarks>
/// <param name="logger">
/// The logger to use for recording diagnostic and operational messages. 
/// Cannot be <see langword="null"/>.
/// </param>
/// <param name="channelOptions">The options to configure the unbounded channel. Cannot be <see langword="null"/>.</param>
/// <exception cref="ArgumentNullException"></exception>
public sealed class UnboundedAsyncEventManager<TArgs>(ILogger logger, UnboundedChannelOptions channelOptions) : AsyncEventManager<TArgs>
{
    private readonly UnboundedChannelOptions _channelOptions = channelOptions ?? throw new ArgumentNullException(nameof(channelOptions));

    /// <inheritdoc/>
    protected override ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    protected override Channel<TArgs> CreateChannel() => Channel.CreateUnbounded<TArgs>(_channelOptions);

    /// <summary>
    /// Invokes all registered event handlers with the specified event arguments.
    /// </summary>
    /// <param name="args">The event data to pass to each event handler.</param>
    /// <returns/>
    /// <inheritdoc cref="AsyncEventManager{TArgs}.Subscribe(AsyncEventHandler{TArgs})"/>
    public void Publish(TArgs args)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        foreach (AsyncEventPublisher<TArgs> publisher in Publishers)
        {
            publisher.TryInvoke(args);
        }
    }
}
