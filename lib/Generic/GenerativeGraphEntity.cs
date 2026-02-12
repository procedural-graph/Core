// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Represents an abstract generative graph entity that supports dynamic composition of components and child
    /// entities, enabling asynchronous generation and regeneration within a graph structure.
    /// </summary>
    /// <inheritdoc/>
    public abstract partial class GenerativeGraphEntity<TKey, TValue> : GraphEntity<TKey, TValue>, IGraphNode
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        internal sealed class DescendantCollection : ICollection<IGraphNode>
        {
            private readonly GenerativeGraphEntity<TKey, TValue> _owner;

            public int Count => _owner.Children.Count + _owner.Components.Count;

            bool ICollection<IGraphNode>.IsReadOnly => false;

            public void Add(IGraphNode item)
            {
                switch (item)
                {
                    case GraphComponent<TKey, TValue> component: _owner.Components.Add(component); break;
                    case GraphEntity<TKey, TValue> entity: _owner.Children.Add(entity); break;
                    default: throw new ArgumentException($"Item must be of type {typeof(GraphComponent<TKey, TValue>).FullName} or {typeof(GraphEntity<TKey, TValue>).FullName}.", nameof(item));
                }
            }

            public bool Contains(IGraphNode item) => item switch
            {
                GraphComponent<TKey, TValue> component => _owner.Components.Contains(component),
                GraphEntity<TKey, TValue> entity => _owner.Children.Contains(entity),
                _ => false
            };

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
                ((ICollection<IGraphNode>)_owner.Children).CopyTo(array, arrayIndex);
            }

            public bool Remove(IGraphNode item) => item switch
            {
                GraphComponent<TKey, TValue> component => _owner.Components.Remove(component),
                GraphEntity<TKey, TValue> entity => _owner.Children.Remove(entity),
                _ => false
            };

            internal DescendantCollection(GenerativeGraphEntity<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public IEnumerator<IGraphNode> GetEnumerator()
            {
                using (ImmutableList<GraphComponent<TKey, TValue>>.Enumerator componentsEnumerator = _owner._components!.GetEnumerator())
                {
                    while (componentsEnumerator.MoveNext())
                    {
                        yield return componentsEnumerator.Current;
                    }
                }

                using ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>.Enumerator childrenEnumerator = _owner.Children.GetEnumerator();
                while (childrenEnumerator.MoveNext())
                {
                    yield return childrenEnumerator.Current;
                }
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

        /// <summary>
        /// The time to wait after a property change before triggering a rebuild, used to prevent excessive re-computation.
        /// </summary>
        protected virtual TimeSpan DebouncePeriod => TimeSpan.FromSeconds(0.2);

        private readonly DescendantCollection _descendants;
        ICollection<IGraphNode> IGraphNode.Descendants => _descendants;

        private ConcurrentList<GraphComponent<TKey, TValue>>? _components;
        /// <summary>
        /// Gets the collection of components associated with this graph entity.
        /// </summary>
        public ICollection<GraphComponent<TKey, TValue>> Components => _components!;

        private Task _componentEventHandler = Task.CompletedTask;

        /// <inheritdoc/>
        public override event Action? Regenerating;

        /// <inheritdoc/>
        public override event Action? Regenerated;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerativeGraphEntity{TKey, TValue}"/> class.
        /// </summary>
        public GenerativeGraphEntity() : base()
        {
            _descendants = new DescendantCollection(this);
        }

        /// <inheritdoc/>
        protected override async ValueTask OnStoppingAsync(CancellationToken cancellationToken)
        {
            ValueTask baseMethod = base.OnStoppingAsync(cancellationToken);
            await baseMethod.ConfigureAwait(false);

            if (!_componentEventHandler.IsCompletedSuccessfully)
            {
                Task eventHandlers = _componentEventHandler.WaitAsync(cancellationToken);
                await eventHandlers.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handles the addition of a new graph component to the entity.
        /// </summary>
        /// <param name="component">The graph component that has been added. Cannot be <see langword="null"/>.</param>
        protected virtual void OnComponentAdded(GraphComponent<TKey, TValue> component)
        {
            component.StateChanged += OnStateChanged;
            OnStateChanged();
        }

        /// <summary>
        /// Handles the removal of a graph component from the entity.
        /// </summary>
        /// <param name="component">The component that has been removed from the graph. Cannot be <see langword="null"/>.</param>
        protected virtual void OnComponentRemoved(GraphComponent<TKey, TValue> component)
        {
            component.StateChanged -= OnStateChanged;
            OnStateChanged();
        }

        /// <summary>
        /// Attempts to initiate a generation operation asynchronously if no other operation is currently in progress.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the generation
        /// was initiated and completed successfully; otherwise, <see langword="false"/>.
        /// </returns>
        protected async ValueTask<bool> TryGenerateAsync(CancellationToken cancellationToken)
        {
            EntityState oldState = SetStateFlag(EntityState.Pending | EntityState.Busy);
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf((oldState & EntityState.Dead) != 0, this);
#else
            if ((oldState & EntityState.Dead) != 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#endif
            if ((oldState & EntityState.Busy) != 0)
            {
                return false;
            }

            try
            {
                await DebounceAsync(cancellationToken);
                return true;
            }
            finally
            {
                ClearStateFlag(EntityState.Busy);
            }
        }

        /// <summary>
        /// Asynchronously generates this entity.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the generation operation.</param>
        /// <returns>A task that represents the asynchronous generation operation.</returns>
        protected abstract Task GenerateAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            try
            {
                if (_components is null)
                {
                    return;
                }

                foreach (IDisposable disposable in _components.OfType<IDisposable>())
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                base.OnDisposing();
            }
        }
    }
}
