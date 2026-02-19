using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Represents an abstract, generic graph structure that manages the instantiation and destruction of graph entities
    /// based on scene member objects. Provides mechanisms for serialization, deserialization, and traversal of graph
    /// elements.
    /// </summary>
    /// <inheritdoc/>
    public abstract partial class Graph<TKey, TValue> : 
        LifecycleGraphNode<TKey, TValue>, 
        IGraphNode,
        IGraph,
        IDictionary<TValue, GraphEntity<TKey, TValue>> 
        where TKey : struct, IEquatable<TKey> 
        where TValue : class
    {
        /// <summary>
        /// Gets the collection of graph converters used to serialize and deserialize graph elements.
        /// </summary>
        public abstract IGraphConverterProvider Converters { get; }

        /// <summary>
        /// Gets the logger instance used to record diagnostic and operational messages for the current node.
        /// </summary>
        public abstract ILogger Logger { get; }

        /// <summary>
        /// Gets the provider that supplies information about scene members for the specified key and value types.
        /// </summary>
        protected abstract ISceneMemberInfoProvider<TKey, TValue> SceneMemberInfoProvider { get; }

        private ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>>? _roots;
        ICollection<IGraphNode> IGraphNode.Descendants => (ICollection<IGraphNode>)_roots!.Values;

        /// <inheritdoc/>
        public ICollection<TValue> Keys => _roots!.Keys;

        /// <inheritdoc/>
        public ICollection<GraphEntity<TKey, TValue>> Values => _roots!.Values;

        /// <inheritdoc/>
        public int Count => _roots!.Count;

        bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.IsReadOnly => false;

        /// <summary>
        /// Gets the graph entity associated with the specified key.
        /// </summary>
        /// <param name="key">The key used to locate the graph entity. Cannot be <see langword="null"/>.</param>
        /// <returns>The graph entity corresponding to the specified key.</returns>
        public GraphEntity<TKey, TValue> this[TValue key] => _roots![key];
        GraphEntity<TKey, TValue> IDictionary<TValue, GraphEntity<TKey, TValue>>.this[TValue key]
        {
            get => _roots![key];
            set => _roots![key] = value;
        }

        /// <inheritdoc/>
        protected override void Stop()
        {
            ValueTask stopTask = StopAsync(CancellationToken.None);
            stopTask.Forget(Logger, this, CancellationToken.None);
        }

        /// <inheritdoc/>
        protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
        {
            _roots = new ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>>();
            return base.BuildCancellationTokenSource(stoppingToken);
        }

        /// <summary>
        /// Attempts to add the specified scene member to the graph.
        /// </summary>
        /// <param name="sceneMember">The scene member to add to the graph. Must not be null.</param>
        /// <param name="entity">
        /// When this method returns, contains the graph entity associated with the added scene member if the operation
        /// succeeded; otherwise, <see langword="null"/>. This parameter is passed uninitialized.
        /// </param>
        /// <returns><see langword="true"/> if the scene member was successfully added to the graph or already exists; otherwise, <see langword="false"/>.</returns>
        public bool Add(TValue sceneMember, out GraphEntity<TKey, TValue>? entity)
        {
            TValue sceneMemberRoot = SceneMemberInfoProvider.GetRoot(sceneMember);
            if (!TryGetOrAddRoot(sceneMemberRoot, out GraphEntity<TKey, TValue>? rootEntity))
            {
                entity = null;
                return false;
            }

            if (sceneMemberRoot == sceneMember)
            {
                entity = rootEntity;
                return true;
            }

            if (Converters.TryFind(sceneMember, out IGraphConverter? converter))
            {
                entity = InsertAsEntity(sceneMember, rootEntity, converter);
                return entity is { };
            }

            entity = null;
            return false;
        }

        private bool TryGetOrAddRoot(TValue sceneRoot, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? entity)
        {
            if (!Converters.TryFind(sceneRoot, out IGraphConverter? converter))
            {
                entity = null;
                return false;
            }

            ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>> roots = _roots!;

            do
            {
                if (roots.TryGetValue(sceneRoot, out entity))
                {
                    return true;
                }

                if (entity is { })
                {
                    continue;
                }

                entity = converter.ToGraph(sceneRoot, this, null) as GraphEntity<TKey, TValue>;
                if (entity is null)
                {
                    return false;
                }
            }
            while (!roots.TryAdd(sceneRoot, entity!));

            entity!.Start(StoppingToken);

            return true;
        }

        private GraphEntity<TKey, TValue>? InsertAsEntity(TValue sceneMember, GraphEntity<TKey, TValue> rootEntity, IGraphConverter converter)
        {
            CancellationToken stoppingToken = rootEntity.StoppingToken;
            TValue parentSceneMember = SceneMemberInfoProvider.GetParent(sceneMember)!;
            using var enumerator = new BreadthFirstGraphTraverser<TKey, TValue>(rootEntity);
            while (enumerator.MoveNext())
            {
                stoppingToken.ThrowIfCancellationRequested();

                GraphEntity<TKey, TValue> current = enumerator.Current;
                TKey currentSceneMemberID = GraphEntity<TKey, TValue>.SceneMemberIdentity(current);

                if (!SceneMemberInfoProvider.Equals(currentSceneMemberID, parentSceneMember))
                {
                    continue;
                }

                if (converter.ToGraph(sceneMember, this, current) is GraphEntity<TKey, TValue> entity)
                {
                    current.Children.Add(entity);
                    entity.Start(StoppingToken);
                    return entity;
                }

                break;
            }

            return null;
        }

        /// <inheritdoc/>
        public bool Remove(TValue item)
        {
            ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>> roots = _roots!;

            TValue sceneRoot = SceneMemberInfoProvider.GetRoot(item);
            if (!roots.TryGetValue(sceneRoot, out GraphEntity<TKey, TValue>? rootEntity))
            {
                return false;
            }

            if (!TryFindParent(sceneRoot, rootEntity, out GraphEntity<TKey, TValue>? parentEntity))
            {
                return false;
            }

            if (parentEntity == rootEntity)
            {
                StopAndDispose(rootEntity);
                return true;
            }

            TKey sceneMemberID = SceneMemberInfoProvider.GetKey(item);
#if NET5_0_OR_GREATER
            if (parentEntity.Children.Remove(sceneMemberID, out IReadOnlySet<GraphEntity<TKey, TValue>>? items))
#else
            if (parentEntity.Children.Remove(sceneMemberID, out IReadOnlyCollection<GraphEntity<TKey, TValue>>? items))
#endif
            {
                using ImmutableHashSet<GraphEntity<TKey, TValue>>.Enumerator enumerator = ((ImmutableHashSet<GraphEntity<TKey, TValue>>)items).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    StopAndDispose(enumerator.Current);
                }
            }

            return false;
        }

        private void StopAndDispose(GraphEntity<TKey, TValue> entity)
        {
            ValueTask stopAndDispose = StopAndDisposeAsync(entity, CancellationToken.None);
            stopAndDispose.Forget(Logger, entity, CancellationToken.None);
        }

        private bool TryFindParent(TValue sceneMember, GraphEntity<TKey, TValue> rootEntity, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? parentEntity)
        {
            TValue? parent = SceneMemberInfoProvider.GetParent(sceneMember);
            TKey parentKey = parent is { } ? SceneMemberInfoProvider.GetKey(parent) : default;

            using var enumerator = new BreadthFirstGraphTraverser<TKey, TValue>(rootEntity);
            while (enumerator.MoveNext())
            {
                GraphEntity<TKey, TValue> current = enumerator.Current;

                if (current is IProxyGraphNode<TValue> proxyNode)
                {
                    TValue? currentParent = SceneMemberInfoProvider.GetParent(proxyNode.SceneMember);
                    if (SceneMemberInfoProvider.Equals(currentParent, parent))
                    {
                        parentEntity = current;
                        return true;
                    }
                }

                if (current.Children.TryGetValue(parentKey, out var results))
                {
                    parentEntity = results.FirstOrDefault();
                    return parentEntity is { };
                }
            }

            parentEntity = null;
            return false;
        }

        private static async ValueTask StopAndDisposeAsync(GraphEntity<TKey, TValue> entity, CancellationToken cancellationToken)
        {
            try
            {
                ValueTask stop = entity.StopAsync(cancellationToken);
                await stop.ConfigureAwait(false);
            }
            finally
            {
                entity.Dispose();
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TValue key)
        {
            return _roots!.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TValue key, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? value)
        {
            return _roots!.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TValue, GraphEntity<TKey, TValue>>> GetEnumerator()
        {
            return _roots!.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _roots!.GetEnumerator();
        }

        void IDictionary<TValue, GraphEntity<TKey, TValue>>.Add(TValue key, GraphEntity<TKey, TValue> value)
        {
            if (_roots!.TryAdd(key, value))
            {
                value.Start(StoppingToken);
            }
        }

        void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Add(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
        {
            ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Add(item);
            item.Value.Start(StoppingToken);
        }

        void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Clear()
        {
            throw new NotSupportedException("Clearing the graph is not supported. Remove individual items instead.");
        }

        bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Contains(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
        {
            return ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Contains(item);
        }

        void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.CopyTo(KeyValuePair<TValue, GraphEntity<TKey, TValue>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Remove(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
        {
            if (((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Remove(item))
            {
                StopAndDispose(item.Value);
                return true;
            }

            return false;
        }
    }
}
