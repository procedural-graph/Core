using System.Threading.Channels;

namespace ProceduralGraph.Events;

internal sealed class DefaultAsyncEvent<TArgs>(ILogger logger) : AsyncEvent<TArgs>
{
    protected override ILogger Logger { get; } = logger;

    protected override Channel<TArgs> CreateChannel() => Channel.CreateUnbounded<TArgs>();
}
