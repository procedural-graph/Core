namespace GameSharp.Events;

public interface IAsyncEventListener<TSender>
{
    bool Invoke(TSender sender, CancellationToken cancellationToken);

    ValueTask InvokeAsync(TSender sender, CancellationToken cancellationToken);
}

public interface IAsyncEventListener<TSender, TEventArgs>
{
    bool Invoke(TSender sender, TEventArgs e, CancellationToken cancellationToken);

    ValueTask InvokeAsync(TSender sender, TEventArgs e, CancellationToken cancellationToken);
}
