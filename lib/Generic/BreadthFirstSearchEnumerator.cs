// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Generic
{
    internal struct BreadthFirstSearchEnumerator<T> : IEnumerator<T> where T : class
    {
        private sealed class PooledQueueResetPolicy : IPooledObjectPolicy<Queue<T>>
        {
            public Queue<T> Create()
            {
                return new Queue<T>();
            }

            public bool Return(Queue<T> obj)
            {
                obj.Clear();
                return true;
            }
        }

        private static readonly ObjectPool<Queue<T>> _queuePool = new DefaultObjectPool<Queue<T>>(new PooledQueueResetPolicy());

        private readonly T _root;
        private readonly Action<T, Queue<T>> _enqueueChildren;
        private Queue<T>? _entities;
        private T? _current;

        public readonly T Current => _current!;

        readonly object? IEnumerator.Current => _current;

        public BreadthFirstSearchEnumerator(T root, Action<T, Queue<T>> enqueueChildren)
        {
            _enqueueChildren = enqueueChildren ?? throw new ArgumentNullException(nameof(enqueueChildren));
            _entities = _queuePool.Get();
            _root = root;
            _current = root;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            _enqueueChildren(_current!, _entities!);
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