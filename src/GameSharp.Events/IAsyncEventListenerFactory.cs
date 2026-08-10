using Microsoft.Extensions.Logging;

namespace GameSharp.Events;

public interface IAsyncEventListenerFactory<TEventArgs>
{
    AsyncEventListener<TSender, TEventArgs> Create<TSender>(AsyncEventHandler<TSender, TEventArgs> handler, ILogger logger);
}
