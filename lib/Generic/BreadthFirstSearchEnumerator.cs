// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Provides a generic enumerator that traverses a graph in breadth-first order, starting from the specified root node.
    /// </summary>
    /// <typeparam name="T">The type of items being enumerated.</typeparam>
    /// <remarks>Internal state can be mutated for performance reasons; therefore, this struct should not be copied.</remarks>
    public struct BreadthFirstSearchEnumerator<T> : IEnumerator<T> where T : class
    {
#if NET5_0_OR_GREATER
        private unsafe delegate*<ref BreadthFirstSearchEnumerator<T>, bool> _iterator;
#else
        private delegate bool IteratorDelegate(ref BreadthFirstSearchEnumerator<T> enumerator);
        private IteratorDelegate _iterator;
#endif
        private readonly T _root;
        private readonly Action<T, Queue<T>> _enqueueChildren;
        private Queue<T>? _entities;
        private T? _current;

        /// <inheritdoc/>
        public readonly T Current => _current!;

        readonly object? IEnumerator.Current => _current;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreadthFirstSearchEnumerator{T}"/> struct starting from the specified root node.
        /// </summary>
        /// <param name="root">The root node from which the breadth-first traversal begins.</param>
        /// <param name="enqueueChildren">A delegate that enqueues the children of a given node into the provided queue.</param>
#if NET5_0_OR_GREATER
        public unsafe BreadthFirstSearchEnumerator(T root, Action<T, Queue<T>> enqueueChildren)
        {
            _iterator = &MoveInit;
#else
        public BreadthFirstSearchEnumerator(T root, Action<T, Queue<T>> enqueueChildren)
        {
            _iterator = MoveInit;
#endif
            _enqueueChildren = enqueueChildren ?? throw new ArgumentNullException(nameof(enqueueChildren));
            _entities = null;
            _root = root;
            _current = root;
        }

        /// <inheritdoc/>
        public unsafe bool MoveNext() => _iterator(ref this);

        /// <inheritdoc/>
#if NET5_0_OR_GREATER
        public unsafe void Reset()
        {
            _iterator = &MoveInit;
#else
        public void Reset()
        {
            _iterator = MoveInit;
#endif
            _entities?.Clear();
            _current = _root;
        }

        private static bool MoveNext(ref BreadthFirstSearchEnumerator<T> enumerator)
        {
            Queue<T> queue = enumerator._entities!;
            if (!queue.TryDequeue(out enumerator._current))
            {
                return false;
            }

            enumerator._enqueueChildren(enumerator._current!, queue);
            return true;
        }

#if NET5_0_OR_GREATER
        private static unsafe bool MoveInit(ref BreadthFirstSearchEnumerator<T> enumerator)
        {
            enumerator._entities = new Queue<T>();
            enumerator._enqueueChildren(enumerator._current!, enumerator._entities);

            if (enumerator._entities.Count == 0)
            {
                enumerator._iterator = &MoveEnd;
            }
            else
            {
                enumerator._iterator = &MoveNext;
            }

            return true;
        }
#else
        private static bool MoveInit(ref BreadthFirstSearchEnumerator<T> enumerator)
        {
            enumerator._entities = new Queue<T>();
            enumerator._enqueueChildren(enumerator._current!, enumerator._entities);

            if (enumerator._entities.Count == 0)
            {
                enumerator._iterator = MoveEnd;
            }
            else
            {
                enumerator._iterator = MoveNext;
            }

            return true;
        }
#endif

        private static unsafe bool MoveEnd(ref BreadthFirstSearchEnumerator<T> enumerator)
        {
            return false;
        }

        readonly void IDisposable.Dispose() { }
    }
}