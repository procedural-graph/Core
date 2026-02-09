// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
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
            _componentEventHandler = HandleCollectionEventsAsync(_components, OnComponentAdded, OnComponentRemoved, Logger, cts.Token);
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
