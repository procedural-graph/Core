namespace GameSharp.Events;

/// <param name="cancellationToken">
/// A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
/// <inheritdoc cref="EventHandler{TSender, TEventArgs}"/>
/// <param name="e"/>
/// <param name="sender"/>
public delegate ValueTask AsyncEventHandler<TSender, TEventArgs>(TSender sender, TEventArgs e, CancellationToken cancellationToken);