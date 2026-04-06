using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Events;

/// <summary>
/// Represents an asynchronous event handler that processes a <typeparamref name="TArgs"/>.
/// </summary>
/// <typeparam name="TArgs">The type of the event arguments.</typeparam>
/// <param name="value">The value to be processed by the event handler.</param>
/// <param name="cancellationToken">
/// A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous operation.
/// </returns>
public delegate ValueTask AsyncEventHandler<TArgs>(TArgs value, CancellationToken cancellationToken);