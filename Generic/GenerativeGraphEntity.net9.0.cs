using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

public abstract partial class GenerativeGraphEntity<TKey, TValue>
{
    /// <inheritdoc/>
    protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
    {
        CancellationTokenSource cts = base.BuildCancellationTokenSource(stoppingToken);
        _components = [];
        _componentEventHandler = HandleCollectionEventsAsync(_components, OnComponentAdded, OnComponentRemoved, Graph.Logger, cts.Token);
        return cts;
    }

    private async Task DebounceAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer periodicTimer = new(DebouncePeriod);

        while (TryClearStateFlag(EntityState.Pending, out _))
        {
            do
            {
                if (await periodicTimer.WaitForNextTickAsync(cancellationToken))
                {
                    continue;
                }

                return;
            }
            while (TryClearStateFlag(EntityState.Pending, out _));

            Regenerating?.Invoke();
            await GenerateAsync(cancellationToken);
            Regenerated?.Invoke();
        }
    }
}
