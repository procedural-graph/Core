using System;
using System.Threading.Channels;

namespace ProceduralGraph.Events;

internal sealed class BoundedAsyncEvent<TArgs>(ILogger logger, BoundedChannelOptions channelOptions) : AsyncEvent<TArgs>
{
    private readonly BoundedChannelOptions _channelOptions = channelOptions;
    private readonly Action<TArgs>? _eventDropped;

    public BoundedAsyncEvent(ILogger logger, BoundedChannelOptions channelOptions, Action<TArgs>? eventDropped) : this(logger, channelOptions)
    {
        _eventDropped = eventDropped;
    }

    protected override ILogger Logger { get; } = logger;

    protected override Channel<TArgs> CreateChannel() => Channel.CreateBounded(_channelOptions, _eventDropped);
}
