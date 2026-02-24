using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph;

/// <summary>
/// Defines a contract for managing the asynchronous start and stop lifecycle of a service or component.
/// </summary>
public interface IAsyncLifecycle
{
    /// <summary>
    /// Gets a token that is triggered when the service is stopping.
    /// </summary>
    CancellationToken StoppingToken { get; }

    /// <summary>
    /// Starts the specified asynchronous lifecycle host.
    /// </summary>
    /// <param name="stoppingToken">Signals that the service should stop.</param>
    void Start(CancellationToken stoppingToken = default);

    /// <summary>
    /// Requests a graceful stop of the asynchronous lifecycle, allowing it to complete any ongoing work before shutting down.
    /// </summary>
    /// <param name="stoppingToken">Communicates that the shutdown should no longer be graceful and that the lifecycle should stop immediately.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the asynchronous lifecycle has fully stopped.</returns>
    ValueTask StopAsync(CancellationToken stoppingToken);
}