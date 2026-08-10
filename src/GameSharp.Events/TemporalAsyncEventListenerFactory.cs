using Microsoft.Extensions.Logging;

namespace GameSharp.Events;

internal sealed class TemporalAsyncEventListenerFactory : IAsyncEventListenerFactory<TimeSpan>
{
    public static TemporalAsyncEventListenerFactory Default { get; } = new();

    public AsyncEventListener<TSender, TimeSpan> Create<TSender>(AsyncEventHandler<TSender, TimeSpan> handler, ILogger logger)
    {
        return new TemporalAsyncEventListener<TSender>(handler, logger);
    }
}
