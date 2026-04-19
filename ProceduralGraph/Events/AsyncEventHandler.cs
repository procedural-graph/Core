using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents an asynchronous event handler that processes a <typeparamref name="TArgs"/>.
/// </summary>
/// <param name="value">The value to be processed by the event handler.</param>
/// <param name="cancellationToken">
/// A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
/// <inheritdoc cref="AsyncEvent{TArgs}"/>
public delegate ValueTask AsyncEventHandler<TArgs>(TArgs value, CancellationToken cancellationToken);