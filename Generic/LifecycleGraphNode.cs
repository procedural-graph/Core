using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents a base class for nodes within a graph structure, providing identity, parent-child relationships, and lifecycle management.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used to identify scene members. Must be a value type that implements 
/// <see cref="IEquatable{TKey}"/>.
/// </typeparam>
/// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
public abstract class LifecycleGraphNode<TKey, TValue> : IGraphNode, IAsyncLifecycle, IDisposable
    where TKey : struct, IEquatable<TKey>
    where TValue : class
{
    IGraphNode? IGraphNode.Parent => null;

    private CancellationTokenSource? _stoppingCts;
    /// <inheritdoc/>
    public CancellationToken StoppingToken => _stoppingCts!.Token;

#if NET8_0_OR_GREATER
    private byte _status;

    private TaskCompletionSource _lifetimeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
#else
    private int _status;

    private TaskCompletionSource<object?> _lifetimeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif

    /// <summary>
    /// Gets a task that represents the lifetime of the current node.
    /// </summary>
    public Task Lifetime => _lifetimeTcs.Task;

    ICollection<IGraphNode> IGraphNode.Descendants => [];

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
    protected abstract void Stop();

    /// <inheritdoc/>
    public void Start(CancellationToken stoppingToken = default)
    {
        bool flagWasSet = TrySetStateFlag(EntityState.Started, out EntityState currentState);
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf((currentState & EntityState.Dead) != 0, this);
#else
        if ((currentState & EntityState.Dead) != 0)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
        if (!flagWasSet)
        {
            return;
        }

        if (_lifetimeTcs.Task.IsCompleted)
        {
#if NET8_0_OR_GREATER
            _lifetimeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
#else
            _lifetimeTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        }

        _stoppingCts = BuildCancellationTokenSource(stoppingToken);
        _stoppingCts.Token.Register(Stop);
    }

    /// <inheritdoc/>
    public async ValueTask StopAsync(CancellationToken stoppingToken = default)
    {
        bool flagWasCleared = TryClearStateFlag(EntityState.Started, out EntityState currentState);
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf((currentState & EntityState.Dead) != 0, this);
#else
        if ((currentState & EntityState.Dead) != 0)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif
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

#if NET8_0_OR_GREATER
        _lifetimeTcs.SetResult();
#else
        _lifetimeTcs.SetResult(null);
#endif
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
