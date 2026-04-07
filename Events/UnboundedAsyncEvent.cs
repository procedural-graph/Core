using System.Threading.Channels;

namespace ProceduralGraph.Events;

internal sealed class UnboundedAsyncEvent<TArgs>(ILogger logger, UnboundedChannelOptions channelOptions) : AsyncEvent<TArgs>
{
    private readonly UnboundedChannelOptions _channelOptions = channelOptions;

    protected override ILogger Logger { get; } = logger;

    protected override Channel<TArgs> CreateChannel() => Channel.CreateUnbounded<TArgs>(_channelOptions);
}
