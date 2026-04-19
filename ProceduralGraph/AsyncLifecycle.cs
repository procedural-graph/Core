using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph;

/// <summary>
/// Provides a base class for managing the asynchronous lifecycle of an operation, including coordinated startup,
/// cancellation, and disposal.
/// </summary>
public abstract class AsyncLifecycle : Disposable, IAsyncDisposable
{
    private CancellationTokenSource? _stoppingCts;
    private Task _lifetime = Task.CompletedTask;

    /// <summary>
    /// Gets a <see cref="CancellationToken"/> that can be used to request cancellation of the asynchronous operation.
    /// </summary>
    public CancellationToken StoppingToken { get; private set; }

    /// <summary>Starts the asynchronous operation.</summary>
    /// <remarks>This method can only be called once.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the asynchronous operation has already been started or disposed.</exception>
    public void Start()
    {
        ThrowHelpers.ThrowIf(!TryStart(), "The asynchronous operation has already been started.");
    }

    /// <summary>
    /// Attempts to start the asynchronous operation if it has not already been started or disposed.
    /// </summary>
    /// <returns><see langword="true"/> if the operation was started; <see langword="false"/> if it was already started or disposed.</returns>
    public bool TryStart()
    {
        if (Disposed || Volatile.Read(ref _stoppingCts) is { })
        {
            return false;
        }

        CancellationTokenSource newCts = new();

        if (Interlocked.CompareExchange(ref _stoppingCts, newCts, null) is { })
        {
            newCts.Dispose();
            return false;
        }

        StoppingToken = newCts.Token;
        _lifetime = RunAsync(StoppingToken);
        return true;
    }

    /// <summary>
    /// Executes the asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">A token to request cancellation of the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that is completed when the asynchronous operation finishes.</returns>
    protected abstract Task RunAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        if (_stoppingCts is null)
        {
            return;
        }

        try
        {
            _stoppingCts.Cancel();
        }
        finally
        {
            _stoppingCts.Dispose();
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        Dispose(disposing: true);
        await _lifetime.ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
