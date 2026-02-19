using Microsoft.Extensions.ObjectPool;
using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Generic
{
    internal struct BreadthFirstGraphTraverser<TKey, TValue> : IEnumerator<GraphEntity<TKey, TValue>>
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        private sealed class PooledQueueResetPolicy : IPooledObjectPolicy<Queue<GraphEntity<TKey, TValue>>>
        {
            public Queue<GraphEntity<TKey, TValue>> Create()
            {
                return new Queue<GraphEntity<TKey, TValue>>();
            }

            public bool Return(Queue<GraphEntity<TKey, TValue>> obj)
            {
                obj.Clear();
                return true;
            }
        }

        private static readonly ObjectPool<Queue<GraphEntity<TKey, TValue>>> _queuePool;

        static BreadthFirstGraphTraverser()
        {
            var resetPolicy = new PooledQueueResetPolicy();
            _queuePool = new DefaultObjectPool<Queue<GraphEntity<TKey, TValue>>>(resetPolicy);
        }

        private readonly GraphEntity<TKey, TValue> _root;
        private Queue<GraphEntity<TKey, TValue>>? _entities;
        private GraphEntity<TKey, TValue>? _current;

        public readonly GraphEntity<TKey, TValue> Current => _current!;

        readonly object? IEnumerator.Current => _current;

        public BreadthFirstGraphTraverser(GraphEntity<TKey, TValue> root)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(root);
#else
            if (root is null)
            { 
                throw new ArgumentNullException(nameof(root));
            }
#endif
            _entities = _queuePool.Get();
            _root = root;
            _current = root;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>? children = _current!.Children;
            int childCount = children.Count;

            if (childCount > 0)
            {
#if NET8_0_OR_GREATER
                _entities!.EnsureCapacity(_entities.Count + childCount);
#endif
                using ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>.Enumerator enumerator = children.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    _entities!.Enqueue(enumerator.Current);
                }
            }

            return _entities!.TryDequeue(out _current);
        }

        public void Reset()
        {
            _entities!.Clear();
            _current = _root;
        }

        public void Dispose()
        {
            if (_entities is null)
            {
                return;
            }

            _queuePool.Return(_entities);
            _entities = null;
        }
    }
}