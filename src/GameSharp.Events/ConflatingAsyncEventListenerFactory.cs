using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace GameSharp.Events;

internal sealed class ConflatingAsyncEventListenerFactory<TEventArgs> : ChannelBackedAsyncEventListenerFactory<TEventArgs>
{
    public static ConflatingAsyncEventListenerFactory<TEventArgs> Default { get; } = new();

    public override AsyncEventListener<TSender, TEventArgs> Create<TSender>(AsyncEventHandler<TSender, TEventArgs> handler, ILogger logger)
    {
        Channel<(TSender, TEventArgs, CancellationToken)> channel = ChannelFactory.CreateConflating<(TSender, TEventArgs, CancellationToken)>();
        return new ChannelBackedAsyncEventListener<TSender, TEventArgs>(handler, channel, logger);
    }
}