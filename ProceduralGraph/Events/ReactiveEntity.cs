using System;
using System.Threading;
using System.Threading.Tasks;

using Mutation = ProceduralGraph.Events.CollectionMutation<ProceduralGraph.Repository, object?>;
#if !NET8_0_OR_GREATER
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource<object?>;
#endif

namespace ProceduralGraph.Events;

/// <summary>
/// Represents a base class for entities within a graph structure, providing identity, parent-child relationships,
/// and lifecycle management.
/// </summary>
/// <inheritdoc/>
public class ReactiveEntity : Entity
{
    private readonly AsyncEventPublisher<Mutation> _changed;
    /// <summary>
    /// Gets the asynchronous event that is raised when a mutation occurs.
    /// </summary>
    public IAsyncEvent<Mutation> Changed => _changed.Event;

    /// <inheritdoc/>
    public ReactiveEntity(ILogger logger) : base(logger)
    {
        _changed = AsyncEventPublisher.Create<Mutation>(Logger);
    }

    /// <inheritdoc/>
    public override bool Add(object item, Type type)
    {
        if (Add(item, type, out Repository previous, out Repository current))
        {
            Mutation mutation = CollectionMutation.Insert(previous, current, item);
            _changed.Publish(mutation);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override bool Remove(object item, Type type)
    {
        if (Remove(item, type, out Repository previous, out Repository current))
        {
            Mutation mutation = CollectionMutation.Delete(previous, current, item);
            _changed.Publish(mutation);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        _ = _changed.Event.Subscribe(OnChangedAsync);
        await base.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Called when the collection of objects associated with this graph entity changes.
    /// </summary>
    /// <inheritdoc cref="AsyncEventHandler{TArgs}"/>
    protected virtual async ValueTask OnChangedAsync(Mutation value, CancellationToken cancellationToken)
    {
        try
        {
            switch (value.Type)
            {
                case CollectionMutationType.Insert: await OnDescendantAddedAsync(value.EventArgs!, cancellationToken);   break;
                case CollectionMutationType.Delete: await OnDescendantRemovedAsync(value.EventArgs!, cancellationToken); break;
            }
        }
        finally
        {
            if (Parent is ReactiveEntity parent)
            {
                await parent.OnDescendantStateChangedAsync(this, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handles logic to be executed asynchronously when a descendant item is added.
    /// </summary>
    /// <param name="item">The descendant item that was added.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    protected virtual async ValueTask OnDescendantAddedAsync(object item, CancellationToken cancellationToken)
    {
        if (item is AsyncLifecycle lifecycle)
        {
            lifecycle.TryStart();
        }
    }

    /// <summary>Handles logic to be executed asynchronously when a descendant item is removed.</summary>
    /// <inheritdoc cref="OnDescendantAddedAsync(object, CancellationToken)"/>
    protected virtual async ValueTask OnDescendantRemovedAsync(object item, CancellationToken cancellationToken) { }

    /// <summary>Handles logic to be executed asynchronously when the state of a descendant entity changes.</summary>
    /// <param name="value">The descendant entity whose state has changed.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    protected virtual async ValueTask OnDescendantStateChangedAsync(ReactiveEntity value, CancellationToken cancellationToken) { }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        try
        {
            _changed.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, this);
        }
    }
}