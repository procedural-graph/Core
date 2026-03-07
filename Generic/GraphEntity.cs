using ProceduralGraph.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents a base class for entities within a graph structure, providing identity, parent-child relationships,
/// and lifecycle management.
/// </summary>
/// <inheritdoc/>
public abstract class GraphEntity<TSceneMember> : LifecycleGraphNode<TSceneMember>, IGraphNode where TSceneMember : class
{
    internal sealed class Comparer : IComparer<GraphEntity<TSceneMember>>
    {
        public int Compare(GraphEntity<TSceneMember>? x, GraphEntity<TSceneMember>? y)
        {
            return Comparer<Guid?>.Default.Compare(x?.ID, y?.ID);
        }
    }

    /// <summary>
    /// Represents the base type for all entity models in a graph structure.
    /// </summary>
    public abstract record Model : GraphComponent<TSceneMember>.Model
    {
        /// <summary>
        /// Gets the unique identifier of the entity associated with this model.
        /// </summary>
        public Guid ID { get; init; }

        /// <summary>
        /// Attempts to retrieve the unique identifier of the scene member associated with this graph entity model.
        /// </summary>
        /// <param name="sceneMember">
        /// When the method returns <see langword="true"/>, contains the scene member; otherwise, 
        /// <see langword="null"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this model has an associated scene member identifier; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool TryGetSceneMember([NotNullWhen(true)] out TSceneMember? sceneMember)
        {
            sceneMember = default;
            return false;
        }
    }

    internal static readonly Comparer comparer = new();

    /// <summary>
    /// Gets the unique identifier for this graph entity.
    /// </summary>
    public abstract Guid ID { get; }

    /// <summary>
    /// Gets the graph this entity belongs to.
    /// </summary>
    protected abstract Graph<TSceneMember> Graph { get; }

    /// <inheritdoc cref="IGraphNode.Parent"/>
    public abstract GraphEntity<TSceneMember>? Parent { get; }
    IGraphNode? IGraphNode.Parent => Parent;

    private ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>? _children;
    /// <inheritdoc cref="IGraphNode.Descendants"/>
    public ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> Children => _children!;
    ICollection<IGraphNode> IGraphNode.Descendants => (ICollection<IGraphNode>)_children!;

    private Task _childEventHandling = Task.CompletedTask;

    /// <summary>
    /// Occurs when the state of the entity has changed.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Occurs when the regeneration process is about to begin.
    /// </summary>
    public abstract event Action? Regenerating;

    /// <summary>
    /// Occurs after the entity has been regenerated.
    /// </summary>
    public abstract event Action? Regenerated;

    /// <inheritdoc/>
    protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
    {
        CancellationToken parentStoppingToken = Parent?.StoppingToken ?? CancellationToken.None;
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, parentStoppingToken);

        _children = new ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>(GetSceneMember);
        _childEventHandling = HandleCollectionEventsAsync(_children, OnChildAdded, OnChildRemoved, Graph.Logger, cts.Token);

        return cts;
    }

    /// <inheritdoc/>
    protected override async ValueTask OnStoppingAsync(CancellationToken stoppingToken)
    {
        ValueTask baseMethod = base.OnStoppingAsync(stoppingToken);
        await baseMethod.ConfigureAwait(false);
        if (_childEventHandling.Status != TaskStatus.RanToCompletion)
        {
            Task wait = _childEventHandling.WaitAsync(stoppingToken);
            await wait.ConfigureAwait(false);
        } 
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        ValueTask stopTask = StopAsync(Graph.StoppingToken);
        stopTask.Forget(Graph.Logger, this, CancellationToken.None);
    }

    /// <summary>
    /// Handles logic to be performed when a child entity is added to the graph.
    /// </summary>
    /// <param name="child">The child entity that has been added. Cannot be <see langword="null"/>.</param>
    protected virtual void OnChildAdded(GraphEntity<TSceneMember> child)
    {
        child.Regenerated += OnStateChanged;
        OnStateChanged();
    }

    /// <summary>
    /// Handles logic to be performed when a child entity is removed from the graph.
    /// </summary>
    /// <param name="child">The child entity that has been removed. Cannot be <see langword="null"/>.</param>
    protected virtual void OnChildRemoved(GraphEntity<TSceneMember> child)
    {
        child.Regenerated -= OnStateChanged;
        OnStateChanged();
    }

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event to notify subscribers that the entity's state has changed.
    /// </summary>
    protected virtual void OnStateChanged()
    {
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Retrieves the scene member associated with this graph entity.
    /// </summary>
    /// <returns>
    /// The scene member associated with this graph entity, or <see langword="null"/> if there is no associated scene member.
    /// </returns>
    protected virtual TSceneMember? GetSceneMember()
    {
        return null;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is GraphEntity<TSceneMember> other && other.ID == ID);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{GetType().Name} ({ID})";
    }

    internal static async Task HandleCollectionEventsAsync<TItem, TEnumerator>(
        ConcurrentCollection<TItem, TEnumerator> collection,
        Action<TItem> onAdded,
        Action<TItem> onRemoved,
        ILogger logger,
        CancellationToken cancellationToken)
        where TEnumerator : IEnumerator<TItem>
    {
        ChannelReader<ItemEventArgs<TItem>> reader = collection.Events;
        await foreach (ItemEventArgs<TItem> args in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                Action<TItem> callback = args.ChangeType switch
                {
                    ItemChangeType.Added => onAdded,
                    ItemChangeType.Removed => onRemoved,
                    _ => throw new InvalidOperationException("Unknown change type.")
                };

                callback(args.Item);
            }
            catch (Exception ex)
            {
                logger.LogException(ex, args.Item);
            }
        }
    }

    internal static TSceneMember? GetSceneMember(GraphEntity<TSceneMember> entity)
    {
        return entity.GetSceneMember();
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        if (_children is null)
        {
            return;
        }

        using ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>.Enumerator enumerator = _children.GetEnumerator();
        while (enumerator.MoveNext())
        {
            GraphEntity<TSceneMember> current = enumerator.Current;
            Task currentLifetime = current.Lifetime;

            if (currentLifetime.IsCompleted)
            {
                if (currentLifetime.IsFaulted)
                {
                    Graph.Logger.LogException(currentLifetime.Exception!, current);
                }

                current.Dispose();
            }

            _ = currentLifetime.ContinueWith(current.Dispose, TaskContinuationOptions.RunContinuationsAsynchronously);
        }
    }

    private void Dispose(Task lifetime)
    {
        try
        {
            Dispose();
            TaskAwaiter taskAwaiter = lifetime.GetAwaiter();
            taskAwaiter.GetResult();
        }
        catch (Exception ex)
        {
            Graph.Logger.LogException(ex, this);
        }
    }
}
