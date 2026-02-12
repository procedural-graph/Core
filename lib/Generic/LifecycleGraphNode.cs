// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Represents a base class for nodes within a graph structure, providing identity, parent-child relationships, and lifecycle management.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key used to identify scene members. Must be a value type that implements 
    /// <see cref="IEquatable{TKey}"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
    public abstract partial class LifecycleGraphNode<TKey, TValue> : IGraphNode, IAsyncLifecycle, IDisposable
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        IGraphNode? IGraphNode.Parent => null;

        private CancellationTokenSource? _stoppingCts;
        /// <inheritdoc/>
        public CancellationToken StoppingToken => _stoppingCts!.Token;

        /// <summary>
        /// Gets a task that represents the lifetime of the current node.
        /// </summary>
        public Task Lifetime => _lifetimeTcs.Task;

        /// <summary>
        /// Gets the logger instance used to record diagnostic and operational messages for the current node.
        /// </summary>
        protected abstract ILogger Logger { get; }

        /// <summary>
        /// Creates a new <see cref = "CancellationTokenSource" /> linked to the stopping token of the specified host.
        /// </summary>
        /// <param name="stoppingToken">Signals that the service should stop.</param>
        /// <returns>
        /// A <see cref="CancellationTokenSource"/> that is linked to the host's stopping token. The returned source will be
        /// canceled when the host's stopping token is canceled.
        /// </returns>
        protected virtual CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            return cts;
        }

        /// <summary>
        /// Called when the node is stopping. This method initiates the shutdown process by signaling cancellation to any ongoing operations.
        /// </summary>
        /// <param name="stoppingToken">Communicates that the shutdown should no longer be graceful and that the lifecycle should stop immediately.</param>
        /// <returns>A <see cref="ValueTask"/> that completes when the asynchronous lifecycle has fully stopped.</returns>
        protected virtual async ValueTask OnStoppingAsync(CancellationToken stoppingToken)
        {
            CancellationTokenSource tcs = _stoppingCts!;
            if (!tcs.IsCancellationRequested)
            {
                Task cancel = tcs.CancelAsync(stoppingToken);
                await cancel.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initiates the stop operation for the specified graph node using the provided cancellation token.
        /// </summary>
        protected virtual void Stop()
        {
            ValueTask stopTask = StopAsync(CancellationToken.None);
            stopTask.Forget(Logger, this, CancellationToken.None);
        }

        private static EntityState OrMutator(EntityState current, EntityState value) => current | value;

        private static EntityState AndMutator(EntityState current, EntityState value) => current & ~value;

        /// <summary>
        /// Called when the node is being disposed. 
        /// This method initiates the disposal process by signaling cancellation to any ongoing operations and releasing resources.
        /// </summary>
        protected virtual void OnDisposing()
        {
            if (_stoppingCts is null)
            {
                return;
            }

            try
            {
                if (!_stoppingCts.IsCancellationRequested)
                {
                    _stoppingCts.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
            finally
            {
                _stoppingCts.Dispose();
            }
        }

        private void Dispose(bool disposing)
        {
            if (TrySetStateFlag(EntityState.Dead, out _) && disposing)
            {
                OnDisposing();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
