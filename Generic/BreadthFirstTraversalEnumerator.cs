using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static ProceduralGraph.Generic.GraphTraverser;

namespace ProceduralGraph.Generic;

/// <summary>
/// Provides a breadth-first traversal of a graph starting from a specified root graph entity.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public struct BreadthFirstTraversalEnumerator<TSceneMember> : IEnumerator<GraphEntity<TSceneMember>> where TSceneMember : class
{
    private readonly GraphEntity<TSceneMember> _root;
    private GraphEntity<TSceneMember>[]? _rentedArray;
    private int _head;
    private int _tail;
    private bool _completed;
    private readonly Func<GraphEntity<TSceneMember>[], ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>, int, int> _add;

    private GraphEntity<TSceneMember>? _current;

    /// <inheritdoc/>
    public readonly GraphEntity<TSceneMember> Current => _current!;
    readonly object? IEnumerator.Current => Current;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreadthFirstTraversalEnumerator{TSceneMember}"/> structure starting from the specified root graph entity.
    /// </summary>
    /// <param name="root">The root graph entity from which the traversal begins. Cannot be <see langword="null"/>.</param>
    /// <param name="preordered">
    /// Determines whether to add children in a preordered manner (sorted by key) or in the order they are encountered. 
    /// Defaults to <see langword="true"/> for preordered traversal.
    /// </param>
    public BreadthFirstTraversalEnumerator(GraphEntity<TSceneMember> root, bool preordered = true)
    {
        ThrowHelpers.ThrowIfNull(root);
        _rentedArray = RentDefaultAllocationSize<GraphEntity<TSceneMember>>();
        _root = root;
        _add = preordered ? AddSortedChildren : AddChildren;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        ThrowHelpers.ThrowIfDisposed(_rentedArray is null, this);
        if (_completed) return false;

        if (_current is null)
        {
            _current = _root;
            return true;
        }

        if (TryGetNonZeroChildren(_current, out ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> children, out int childCount))
        {
            int currentSize = _tail - _head;
            int requiredCapacity = currentSize + childCount;
            if ((_tail + childCount) > _rentedArray.Length)
            {
                if (requiredCapacity > _rentedArray.Length)
                {
                    Grow(requiredCapacity, ref _rentedArray, currentSize, _head);
                }
                else
                {
                    if (currentSize > 0)
                    {
                        Array.Copy(_rentedArray, _head, _rentedArray, 0, currentSize);
                    }

                    Array.Clear(_rentedArray, currentSize, _head);
                }

                _tail = currentSize;
                _head = 0;
            }

            _tail += _add(_rentedArray, children, _tail);
        }

        if (_head < _tail)
        {
            _current = Pop(_rentedArray, _head++);
            return true;
        }

        _completed = true;
        _current = null;
        return false;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        ThrowHelpers.ThrowIfDisposed(_rentedArray is null, this);
        Array.Clear(_rentedArray, 0, _tail);
        _completed = false;
        _tail = 0;
        _current = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Return(_rentedArray))
        {
            _rentedArray = null;
        }
    }
}