using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace GameSharp.Events;

internal sealed class QueueAsyncEventListenerFactory<TEventArgs> : ChannelBackedAsyncEventListenerFactory<TEventArgs>
{
    public static QueueAsyncEventListenerFactory<TEventArgs> Default { get; } = new();

    public override AsyncEventListener<TSender, TEventArgs> Create<TSender>(AsyncEventHandler<TSender, TEventArgs> handler, ILogger logger)
    {
        Channel<(TSender, TEventArgs, CancellationToken)> channel = ChannelFactory.CreateQueue<(TSender, TEventArgs, CancellationToken)>();
        return new ChannelBackedAsyncEventListener<TSender, TEventArgs>(handler, channel, logger);
    }
}
