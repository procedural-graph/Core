using Microsoft.Extensions.Logging;

namespace GameSharp.Events;

internal abstract class ChannelBackedAsyncEventListenerFactory<TEventArgs> : IAsyncEventListenerFactory<TEventArgs>
{
    public abstract AsyncEventListener<TSender, TEventArgs> Create<TSender>(AsyncEventHandler<TSender, TEventArgs> handler, ILogger logger);
}
