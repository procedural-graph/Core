using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents a base class for graph entities that manage a collection of components and child entities.
/// </summary>
/// <inheritdoc/>
public abstract class ComponentGraphEntity<TSceneMember> : GraphEntity<TSceneMember>, IGraphNode where TSceneMember : class
{
    /// <summary>
    /// Represents a collection of all immediate descendant nodes, including both components and child entities, of
    /// a generative graph entity.
    /// </summary>
    public sealed class DescendantCollection : ICollection<IGraphNode>
    {
        /// <summary>
        /// Enumerates the nodes within a generative graph entity, providing sequential access to its components and
        /// child entities as graph nodes.
        /// </summary>
        public struct Enumerator : IEnumerator<IGraphNode>
        {
            private readonly ComponentGraphEntity<TSceneMember> _owner;
            private readonly IEnumerator<GraphComponent<TSceneMember>> _componentsEnumerator;
            private readonly ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>.Enumerator _childrenEnumerator;
            private IGraphNode? _current;
            /// <inheritdoc/>
            public readonly IGraphNode Current => _current!;
            readonly object IEnumerator.Current => Current;
            internal Enumerator(ComponentGraphEntity<TSceneMember> owner)
            {
                _owner = owner;
                _componentsEnumerator = owner.Components.GetEnumerator();
                _childrenEnumerator = owner.Children.GetEnumerator();
                _current = null;
            }
            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (_componentsEnumerator.MoveNext())
                {
                    _current = _componentsEnumerator.Current;
                    return true;
                }
                if (_childrenEnumerator.MoveNext())
                {
                    _current = _childrenEnumerator.Current;
                    return true;
                }
                return false;
            }
            /// <inheritdoc/>
            public void Reset()
            {
                _componentsEnumerator.Reset();
                _childrenEnumerator.Reset();
                _current = null!;
            }
            /// <inheritdoc/>
            public readonly void Dispose()
            {
                _componentsEnumerator.Dispose();
                _childrenEnumerator.Dispose();
            }
        }

        private readonly ComponentGraphEntity<TSceneMember> _owner;

        /// <inheritdoc/>
        public int Count => _owner.Children.Count + _owner.Components.Count;

        bool ICollection<IGraphNode>.IsReadOnly => false;

        /// <inheritdoc/>
        public void Add(IGraphNode item)
        {
            switch (item)
            {
                case GraphComponent<TSceneMember> component: _owner.Components.Add(component); break;
                case GraphEntity<TSceneMember> entity: _owner.Children.Add(entity); break;
                default: throw new ArgumentException($"Item must be of type {typeof(GraphComponent<TSceneMember>).FullName} or {typeof(GraphEntity<TSceneMember>).FullName}.", nameof(item));
            }
        }

        /// <inheritdoc/>
        public bool Contains(IGraphNode item) => item switch
        {
            GraphComponent<TSceneMember> component => _owner.Components.Contains(component),
            GraphEntity<TSceneMember> entity => _owner.Children.Contains(entity),
            _ => false
        };

        /// <inheritdoc/>
        public void CopyTo(IGraphNode[] array, int arrayIndex)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(array, nameof(array));
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex, nameof(arrayIndex));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(arrayIndex, array.Length, nameof(arrayIndex));
#else
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0 || arrayIndex >= array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
#endif

            ICollection<IGraphNode> components = ((ICollection<IGraphNode>)_owner.Components);
            components.CopyTo(array, arrayIndex);
            arrayIndex += components.Count;
            ((ICollection<IGraphNode>)(ICollection<GraphEntity<TSceneMember>>)_owner.Children).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(IGraphNode item) => item switch
        {
            GraphComponent<TSceneMember> component => _owner.Components.Remove(component),
            GraphEntity<TSceneMember> entity => _owner.Children.Remove(entity),
            _ => false
        };

        internal DescendantCollection(ComponentGraphEntity<TSceneMember> owner)
        {
            _owner = owner;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_owner);
        }

        IEnumerator<IGraphNode> IEnumerable<IGraphNode>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<IGraphNode>.Clear()
        {
            throw new NotSupportedException("Clearing the collection of descendants is not supported. Remove individual components and child entities instead.");
        }
    }

    private readonly DescendantCollection _descendants;
    ICollection<IGraphNode> IGraphNode.Descendants => _descendants;

    private ConcurrentList<GraphComponent<TSceneMember>>? _components;
    /// <summary>
    /// Gets the collection of components associated with this graph entity.
    /// </summary>
    public ConcurrentList<GraphComponent<TSceneMember>> Components => _components!;

    private CollectionChangeEventHandler<GraphComponent<TSceneMember>>? _componentEventHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentGraphEntity{TSceneMember}"/> class.
    /// </summary>
    public ComponentGraphEntity() : base()
    {
        _descendants = new DescendantCollection(this);
    }

    /// <inheritdoc/>
    protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
    {
        CancellationTokenSource cts = base.BuildCancellationTokenSource(stoppingToken);
        _components = new ConcurrentList<GraphComponent<TSceneMember>>(Graph.Logger);
        _componentEventHandler = new CollectionChangeEventHandler<GraphComponent<TSceneMember>>(_components, OnComponentAdded, OnComponentRemoved);
        return cts;
    }

    /// <summary>
    /// Handles the addition of a new graph component to the entity.
    /// </summary>
    /// <param name="component">The graph component that has been added. Cannot be <see langword="null"/>.</param>
    protected virtual void OnComponentAdded(GraphComponent<TSceneMember> component)
    {
        component.StateChanged += OnStateChanged;
        OnStateChanged();
    }

    /// <summary>
    /// Handles the removal of a graph component from the entity.
    /// </summary>
    /// <param name="component">The component that has been removed from the graph. Cannot be <see langword="null"/>.</param>
    protected virtual void OnComponentRemoved(GraphComponent<TSceneMember> component)
    {
        component.StateChanged -= OnStateChanged;
        OnStateChanged();
    }

    /// <inheritdoc/>
    protected override async ValueTask OnStoppingAsync(CancellationToken cancellationToken)
    {
        ValueTask baseMethod = base.OnStoppingAsync(cancellationToken);
        if (_componentEventHandler is { })
        {
            ValueTask unsubscribe = _componentEventHandler.DisposeAsync();
            await unsubscribe.ConfigureAwait(false);
        }
        await baseMethod.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        try
        {
            if (_components is null)
            {
                return;
            }

            foreach (GraphComponent<TSceneMember> component in _components)
            {
                if (component is IDisposable disposableComponent)
                {
                    disposableComponent.Dispose();
                }
            }

            _components.Dispose();
        }
        finally
        {
            base.OnDisposing();
        }
    }
}
