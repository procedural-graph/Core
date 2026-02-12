// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    public abstract partial class LifecycleGraphNode<TKey, TValue>
    {
        private int _status;

        private TaskCompletionSource<object?> _lifetimeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        ICollection<IGraphNode> IGraphNode.Descendants => ImmutableArray<IGraphNode>.Empty;

        /// <inheritdoc/>
        public void Start(CancellationToken stoppingToken = default)
        {
            bool flagWasSet = TrySetStateFlag(EntityState.Started, out EntityState currentState);
            if ((currentState & EntityState.Dead) != 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (flagWasSet)
            {
                if (_lifetimeTcs.Task.IsCompleted)
                {
                    _lifetimeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                _stoppingCts = BuildCancellationTokenSource(stoppingToken);
                _stoppingCts.Token.Register(Stop);
            }
        }

        /// <inheritdoc/>
        public async ValueTask StopAsync(CancellationToken stoppingToken = default)
        {
            bool flagWasCleared = TryClearStateFlag(EntityState.Started, out EntityState currentState);

            if ((currentState & EntityState.Dead) != 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!flagWasCleared)
            {
                await _lifetimeTcs.Task.ConfigureAwait(false);
                return;
            }

            try
            {
                ValueTask stop = OnStoppingAsync(stoppingToken);
                await stop.ConfigureAwait(false);
                _lifetimeTcs.SetResult(null);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                _lifetimeTcs.TrySetException(ex);
                throw;
            }
        }

        /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/TrySetStateFlag/*' />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TrySetStateFlag(EntityState value, out EntityState resultantState)
        {
            return (MutateState(value, OrMutator, out resultantState) & value) == 0;
        }

        /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/SetStateFlag/*' />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected EntityState SetStateFlag(EntityState value)
        {
            return MutateState(value, OrMutator, out _);
        }

        /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/TryClearStateFlag/*' />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryClearStateFlag(EntityState value, out EntityState resultantState)
        {
            return (MutateState(value, AndMutator, out resultantState) & value) != 0;
        }

        /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/ClearStateFlag/*' />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected EntityState ClearStateFlag(EntityState value)
        {
            return MutateState(value, AndMutator, out _);
        }

        private EntityState MutateState(EntityState value, Func<EntityState, EntityState, EntityState> mutator, out EntityState newState)
        {
            EntityState currentState = (EntityState)Volatile.Read(ref _status);
            EntityState oldState;
            while (true)
            {
                newState = mutator(currentState, value);
                oldState = (EntityState)Interlocked.CompareExchange(ref _status, (byte)newState, (byte)currentState);
                if (oldState == currentState)
                {
                    break;
                }
                currentState = oldState;
            }
            return oldState;
        }
    }
}
