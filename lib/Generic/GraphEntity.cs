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
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Represents a base class for entities within a graph structure, providing identity, parent-child relationships,
    /// and lifecycle management.
    /// </summary>
    /// <inheritdoc/>
    public abstract partial class GraphEntity<TKey, TValue> : 
        LifecycleGraphNode<TKey, TValue>, 
        IEquatable<GraphEntity<TKey, TValue>>,
        IGraphNode
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        /// <summary>
        /// Gets the unique identifier for this graph entity.
        /// </summary>
        public abstract Guid ID { get; }

        /// <summary>
        /// Gets the graph this entity belongs to.
        /// </summary>
        protected abstract Graph<TKey, TValue> Graph { get; }

        /// <summary>
        /// Gets the parent entity of the current graph entity, or <see langword="null"/> if the entity has no parent.
        /// </summary>
        public abstract GraphEntity<TKey, TValue>? Parent { get; }
        IGraphNode? IGraphNode.Parent => Parent;

        internal ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>? childEntities;
        IReadOnlyCollection<IGraphNode> IGraphNode.Descendants => (IReadOnlyCollection<IGraphNode>)childEntities!;

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

            childEntities = new ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>(SceneMemberIdentity);
            _childEventHandling = HandleCollectionEventsAsync(childEntities, OnChildAdded, OnChildRemoved, Logger, cts.Token);

            return cts;
        }

        /// <inheritdoc/>
        protected override async ValueTask OnStoppingAsync(CancellationToken stoppingToken)
        {
            ValueTask baseMethod = base.OnStoppingAsync(stoppingToken);
            await baseMethod.ConfigureAwait(false);

            if (!_childEventHandling.IsCompletedSuccessfully)
            {
                Task wait = _childEventHandling.WaitAsync(stoppingToken);
                await wait.ConfigureAwait(false);
            } 
        }

        /// <inheritdoc/>
        protected override void Stop()
        {
            ValueTask stopTask = StopAsync(Graph.StoppingToken);
            stopTask.Forget(Logger, this, CancellationToken.None);
        }

        /// <summary>
        /// Handles logic to be performed when a child entity is added to the graph.
        /// </summary>
        /// <param name="child">The child entity that has been added. Cannot be <see langword="null"/>.</param>
        protected virtual void OnChildAdded(GraphEntity<TKey, TValue> child)
        {
            child.Regenerated += OnStateChanged;
            OnStateChanged();
        }

        /// <summary>
        /// Handles logic to be performed when a child entity is removed from the graph.
        /// </summary>
        /// <param name="child">The child entity that has been removed. Cannot be <see langword="null"/>.</param>
        protected virtual void OnChildRemoved(GraphEntity<TKey, TValue> child)
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
        /// Retrieves the unique identifier of the scene member associated with this graph entity.
        /// </summary>
        /// <returns>
        /// The <typeparamref name="TKey"/> of the scene member associated with this graph entity, 
        /// or the default value of <typeparamref name="TKey"/> if not applicable.
        /// </returns>
        protected virtual TKey SceneMemberIdentity()
        {
            return default;
        }

        /// <inheritdoc/>
        public bool Equals(GraphEntity<TKey, TValue>? other)
        {
            return ReferenceEquals(this, other) || other is { } && other.ID == ID;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GraphEntity<TKey, TValue> other && Equals(other);
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

        internal static async Task HandleCollectionEventsAsync<T>(
            ConcurrentCollection<T> collection,
            Action<T> onAdded,
            Action<T> onRemoved,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            ChannelReader<ItemEventArgs<T>> reader = collection.Events;
            await foreach (ItemEventArgs<T> args in reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    Action<T> callback = args.ChangeType switch
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

        internal static TKey SceneMemberIdentity(GraphEntity<TKey, TValue> entity)
        {
            return entity.SceneMemberIdentity();
        }

        internal static void EnqueueChildren(GraphEntity<TKey, TValue> entity, Queue<GraphEntity<TKey, TValue>> queue)
        {
            ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>? children = entity.childEntities;
            if (children?.Count is int childCount && childCount > 0)
            {
#if NET8_0_OR_GREATER
                queue.EnsureCapacity(queue.Count + childCount);
#endif
                using var enumerator = children.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    queue.Enqueue(enumerator.Current);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            base.OnDisposing();

            if (childEntities is null)
            {
                return;
            }

            using ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>.Enumerator enumerator = childEntities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                GraphEntity<TKey, TValue> current = enumerator.Current;
                Task currentLifetime = current.Lifetime;

                if (currentLifetime.IsCompleted)
                {
                    if (currentLifetime.IsFaulted)
                    {
                        Logger.LogException(currentLifetime.Exception!, current);
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
                Logger.LogException(ex, this);
            }
        }
    }
}
