using ProceduralGraph.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    public abstract partial class GenerativeGraphEntity<TKey, TValue>
    {
        /// <inheritdoc/>
        protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
        {
            CancellationTokenSource cts = base.BuildCancellationTokenSource(stoppingToken);
            _components = new ConcurrentList<GraphComponent<TKey, TValue>>();
            _componentEventHandler = HandleCollectionEventsAsync(_components, OnComponentAdded, OnComponentRemoved, Graph.Logger, cts.Token);
            return cts;
        }

        private async Task DebounceAsync(CancellationToken cancellationToken)
        {
            while (TryClearStateFlag(EntityState.Pending, out _))
            {
                do
                {
                    try
                    {
                        Task delay = Task.Delay(DebouncePeriod, cancellationToken);
                        await delay.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
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
}
