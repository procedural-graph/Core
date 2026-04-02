using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents an abstract generative graph entity that supports dynamic composition of components and child
/// entities, enabling asynchronous generation and regeneration within a graph structure.
/// </summary>
/// <inheritdoc/>
public abstract class GenerativeGraphEntity<TSceneMember>() : ComponentGraphEntity<TSceneMember>() where TSceneMember : class
{
    /// <summary>
    /// The time to wait after a property change before triggering a rebuild, used to prevent excessive re-computation.
    /// </summary>
    protected virtual TimeSpan DebouncePeriod => TimeSpan.FromSeconds(0.2);

    /// <inheritdoc/>
    public override event Action? Regenerating;

    /// <inheritdoc/>
    public override event Action? Regenerated;

    /// <summary>
    /// Attempts to initiate a generation operation asynchronously if no other operation is currently in progress.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the generation
    /// was initiated and completed successfully; otherwise, <see langword="false"/>.
    /// </returns>
    protected async ValueTask<bool> TryGenerateAsync(CancellationToken cancellationToken)
    {
        EntityState oldState = SetStateFlag(EntityState.Pending | EntityState.Busy);
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf((oldState & EntityState.Dead) != 0, this);
#else
        if ((oldState & EntityState.Dead) != 0)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
        if ((oldState & EntityState.Busy) != 0)
        {
            return false;
        }

        try
        {
            await DebounceAsync(cancellationToken);
            return true;
        }
        finally
        {
            ClearStateFlag(EntityState.Busy);
        }
    }

    /// <summary>
    /// Asynchronously generates this entity.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the generation operation.</param>
    /// <returns>A task that represents the asynchronous generation operation.</returns>
    protected abstract Task GenerateAsync(CancellationToken cancellationToken);

    private async Task DebounceAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer periodicTimer = new(DebouncePeriod);
        while (TryClearStateFlag(EntityState.Pending, out _))
        {
            do
            {
                if (!await periodicTimer.WaitForNextTickAsync(cancellationToken))
                {
                    return;
                }
            }
            while (TryClearStateFlag(EntityState.Pending, out _));
            Regenerating?.Invoke();
            await GenerateAsync(cancellationToken);
            Regenerated?.Invoke();
        }
    }
}
