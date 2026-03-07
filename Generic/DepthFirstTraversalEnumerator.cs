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
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public ref struct DepthFirstGraphTraverser<TSceneMember> : IEnumerator<GraphEntity<TSceneMember>> where TSceneMember : class
{
    private readonly GraphEntity<TSceneMember> _root;
    private GraphEntity<TSceneMember>[]? _rentedArray;
    private int _count;
    private bool _completed;
    private readonly Func<GraphEntity<TSceneMember>[], ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>, int, int> _add;

    private GraphEntity<TSceneMember>? _current;

    /// <inheritdoc/>
    public readonly GraphEntity<TSceneMember> Current => _current!;
    readonly object? IEnumerator.Current => Current;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepthFirstGraphTraverser{TSceneMember}"/> structure starting from the specified root graph entity.
    /// </summary>
    /// <param name="root">The root graph entity from which the traversal begins. Cannot be <see langword="null"/>.</param>
    /// <param name="preordered">
    /// Determines whether to add children in a preordered manner (sorted by key) or in the order they are encountered. 
    /// Defaults to <see langword="true"/> for preordered traversal.
    /// </param>
    public DepthFirstGraphTraverser(GraphEntity<TSceneMember> root, bool preordered = true)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(root);
#else
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }
#endif
        _rentedArray = RentDefaultAllocationSize<GraphEntity<TSceneMember>>();
        _root = root;
        _add = preordered ? AddSortedChildren : AddChildren;
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

        if (TryGetNonZeroChildren(_current, out ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> children, out int childCount))
        {
            int newCount = _count + childCount;
            if (newCount > _rentedArray.Length)
            {
                Grow(newCount, ref _rentedArray, _count);
            }
            _count += _add(_rentedArray, children, _count);
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
            throw new ObjectDisposedException(nameof(DepthFirstGraphTraverser<>));
        }
    }
}