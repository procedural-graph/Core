// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

public abstract partial class LifecycleGraphNode<TKey, TValue>
{
    private byte _status;

    private TaskCompletionSource _lifetimeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    IReadOnlyCollection<IGraphNode> IGraphNode.Descendants => [];

    /// <inheritdoc/>
    public void Start(CancellationToken stoppingToken = default)
    {
        bool flagWasSet = TrySetStateFlag(EntityState.Started, out EntityState currentState);
        ObjectDisposedException.ThrowIf((currentState & EntityState.Dead) != 0, this);
        if (!flagWasSet)
        {
            return;
        }

        if (_lifetimeTcs.Task.IsCompleted)
        {
            _lifetimeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _stoppingCts = BuildCancellationTokenSource(stoppingToken);
        _stoppingCts.Token.Register(Stop);
    }

    /// <inheritdoc/>
    public async ValueTask StopAsync(CancellationToken stoppingToken = default)
    {
        bool flagWasCleared = TryClearStateFlag(EntityState.Started, out EntityState currentState);
        ObjectDisposedException.ThrowIf((currentState & EntityState.Dead) != 0, this);
        if (!flagWasCleared)
        {
            Task wait = _lifetimeTcs.Task.WaitAsync(stoppingToken);
            await wait.ConfigureAwait(false);
            return;
        }

        try
        {
            ValueTask stop = OnStoppingAsync(stoppingToken);
            await stop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            _lifetimeTcs.SetException(ex);
            throw;
        }

        _lifetimeTcs.SetResult();
    }

    /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/TrySetStateFlag/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TrySetStateFlag(EntityState value, out EntityState resultantState)
    {
        return (MutateState(value, &OrMutator, out resultantState) & value) == 0;
    }

    /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/SetStateFlag/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe EntityState SetStateFlag(EntityState value)
    {
        return MutateState(value, &OrMutator, out _);
    }

    /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/TryClearStateFlag/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryClearStateFlag(EntityState value, out EntityState resultantState)
    {
        return (MutateState(value, &AndMutator, out resultantState) & value) != 0;
    }

    /// <include file='LifecycleGraphNode.cs.xml' path='doc/members[@name="LifecycleGraphNode"]/ClearStateFlag/*' />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe EntityState ClearStateFlag(EntityState value)
    {
        return MutateState(value, &AndMutator, out _);
    }

    private unsafe EntityState MutateState(EntityState value, delegate*<EntityState, EntityState, EntityState> mutator, out EntityState newState)
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
