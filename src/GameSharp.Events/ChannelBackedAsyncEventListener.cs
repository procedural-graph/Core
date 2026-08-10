using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace GameSharp.Events;

internal class ChannelBackedAsyncEventListener<TSender, TEventArgs>(
    AsyncEventHandler<TSender, TEventArgs> eventHandler,
    Channel<(TSender, TEventArgs, CancellationToken)> channel,
    ILogger logger) : AsyncEventListener<TSender, TEventArgs>(eventHandler), IAsyncEventListener<TSender, TEventArgs>
{
    public bool Invoke(TSender sender, TEventArgs e, CancellationToken cancellationToken)
    {
        return channel.Writer.TryWrite((sender, e, cancellationToken));
    }

    public ValueTask InvokeAsync(TSender sender, TEventArgs e, CancellationToken cancellationToken)
    {
        return channel.Writer.WriteAsync((sender, e, cancellationToken), cancellationToken);
    }

    protected override async Task ExecuteAsync()
    {
        await foreach ((TSender sender, TEventArgs e, CancellationToken cancellationToken) in channel.Reader.ReadAllAsync(StoppingToken))
        {
            try
            {
                await EventHandler(sender, e, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                EventHandlerThrewAnException(logger, ex);
            }
        }
    }

    protected override async Task OnStoppingAsync()
    {
        channel.Writer.Complete();
        Task execution = base.OnStoppingAsync();
        await execution.ConfigureAwait(false);
    }
}
