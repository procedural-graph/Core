using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static ProceduralGraph.Generic.GraphTraverser;

namespace ProceduralGraph.Generic;

/// <summary>
/// Provides a depth-first traversal of a graph starting from a specified root graph entity.
/// </summary>
public ref struct DepthFirstGraphTraverser<TKey, TValue> : IEnumerator<GraphEntity<TKey, TValue>>
    where TKey : struct, IEquatable<TKey>
    where TValue : class
{
    private readonly GraphEntity<TKey, TValue> _root;
    private GraphEntity<TKey, TValue>[]? _rentedArray;
    private int _count;
    private bool _completed;

    private GraphEntity<TKey, TValue>? _current;

    /// <inheritdoc/>
    public readonly GraphEntity<TKey, TValue> Current => _current!;
    readonly object? IEnumerator.Current => Current;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepthFirstGraphTraverser{TKey, TValue}"/> structure starting from the specified root graph entity.
    /// </summary>
    /// <param name="root">The root graph entity from which the traversal begins. Cannot be <see langword="null"/>.</param>
    public DepthFirstGraphTraverser(GraphEntity<TKey, TValue> root)
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

        if (_completed)
        {
            return false;
        }

        if (_current is null)
        {
            _current = _root;
            return true;
        }

        if (TryGetNonZeroChildren(_current, out ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>> children, out int childCount))
        {
            int newCount = _count + childCount;
            if (newCount > _rentedArray.Length)
            {
                Grow(newCount, ref _rentedArray, _count);
            }
            _count += AddSortedChildren(_rentedArray, children, _count);
        }

        if (_count > 0)
        {
            _current = Pop(_rentedArray, --_count);
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
        Array.Clear(_rentedArray, 0, _count);
        _completed = false;
        _count = 0;
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
            throw new ObjectDisposedException(nameof(DepthFirstGraphTraverser<,>));
        }
    }
}