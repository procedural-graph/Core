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
/// <inheritdoc cref="LifecycleGraphNode{TKey, TValue}"/>
public ref struct BreadthFirstTraversalEnumerator<TKey, TValue> : IEnumerator<GraphEntity<TKey, TValue>>
    where TKey : struct, IEquatable<TKey>
    where TValue : class
{
    private readonly GraphEntity<TKey, TValue> _root;
    private GraphEntity<TKey, TValue>[]? _rentedArray;
    private int _head;
    private int _tail;
    private bool _completed;

    private GraphEntity<TKey, TValue>? _current;

    /// <inheritdoc/>
    public readonly GraphEntity<TKey, TValue> Current => _current!;
    readonly object? IEnumerator.Current => Current;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreadthFirstTraversalEnumerator{TKey, TValue}"/> structure starting from the specified root graph entity.
    /// </summary>
    /// <param name="root">The root graph entity from which the traversal begins. Cannot be <see langword="null"/>.</param>
    public BreadthFirstTraversalEnumerator(GraphEntity<TKey, TValue> root)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(root);
#else
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }
#endif
        _rentedArray = RentDefaultAllocationSize<GraphEntity<TKey, TValue>>();
        _root = root;
    }

    /// <inheritdoc/>
    public bool MoveNext()
    {
        ThrowObjectDisposedExceptionIf(_rentedArray is null);

        if (_completed) return false;

        if (_current is null)
        {
            _current = _root;
            return true;
        }

        if (TryGetNonZeroChildren(_current, out ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>> children, out int childCount))
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

            _tail += AddSortedChildren(_rentedArray, children, _tail);
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
        ThrowObjectDisposedExceptionIf(_rentedArray is null);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowObjectDisposedExceptionIf([DoesNotReturnIf(true)] bool condition)
    {
        if (condition)
        {
            throw new ObjectDisposedException(nameof(BreadthFirstTraversalEnumerator<,>));
        }
    }
}