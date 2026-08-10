using System.Threading.Channels;

namespace GameSharp.Events;

internal static class ChannelFactory
{
    private static readonly BoundedChannelOptions _conflatingChannelOptions = new(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    };

    private static readonly UnboundedChannelOptions _queueChannelOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    public static Channel<T> CreateQueue<T>() => Channel.CreateUnbounded<T>(_queueChannelOptions);

    public static Channel<T> CreateConflating<T>() => Channel.CreateBounded<T>(_conflatingChannelOptions);
}