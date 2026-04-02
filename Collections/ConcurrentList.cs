using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ProceduralGraph.Collections;

/// <summary>
/// Represents a thread-safe, mutable list that supports concurrent add, remove, and update operations.
/// </summary>
/// <typeparam name="T">The type of elements contained in the list.</typeparam>
public class ConcurrentList<T> : ConcurrentCollection<T, ImmutableList<T>.Enumerator>, ICollection<T>, IList<T>
{
    private ImmutableList<T> _items;

    private readonly IEqualityComparer<T> _comparer;

    /// <inheritdoc/>
    public override int Count => _items.Count;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that contains elements copied from the specified
    /// collection and uses the specified equality comparer for item comparisons.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    /// <param name="comparer">The equality comparer to use for comparing items in the list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    public ConcurrentList(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        ThrowHelpers.ThrowIfNull(collection);
        _items = [.. collection];

        ThrowHelpers.ThrowIfNull(comparer);
        _comparer = comparer;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that contains elements copied from the specified
    /// collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    public ConcurrentList(IEnumerable<T> collection)
    {
        ThrowHelpers.ThrowIfNull(collection);
        _comparer = EqualityComparer<T>.Default;
        _items = [.. collection];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class using the specified equality comparer.
    /// </summary>
    /// <param name="comparer">The equality comparer to use for comparing items in the list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    public ConcurrentList(IEqualityComparer<T> comparer)
    {
        ThrowHelpers.ThrowIfNull(comparer);
        _comparer = comparer;
        _items = [];
    }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="ConcurrentList{T}"/> class using the default equality 
    /// comparer of <typeparamref name="T"/>.
    /// </summary>
    public ConcurrentList()
    {
        _comparer = EqualityComparer<T>.Default;
        _items = [];
    }

    /// <inheritdoc/>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            ThrowHelpers.ThrowIf(IsCompleted, ModificationAfterCompletionError);

            T oldItem;

            ImmutableList<T>? oldList, currentList = _items;
            do
            {
                oldList = currentList;
                ThrowHelpers.ThrowIfOutOfRange(index, oldList.Count);
                oldItem = oldList[index];
                ImmutableList<T> newList = oldList.Replace(oldItem, value, _comparer);
                if (ReferenceEquals(newList, oldList))
                {
                    return;
                }
                currentList = Interlocked.CompareExchange(ref _items, newList, oldList);
            }
            while (!ReferenceEquals(currentList, oldList));

            RaiseCollectionChanged(oldItem, ItemChangeType.Removed);
            RaiseCollectionChanged(value, ItemChangeType.Added);
        }
    }

    /// <inheritdoc/>
    public void Add(T item)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIf(IsCompleted, ModificationAfterCompletionError);

        ImmutableList<T>? oldList, currentList = _items;
        do
        {
            oldList = currentList;
            ImmutableList<T> newList = oldList.Add(item);
            currentList = Interlocked.CompareExchange(ref _items, newList, oldList);
        }
        while (!ReferenceEquals(currentList, oldList));

        RaiseCollectionChanged(item, ItemChangeType.Added);
    }

    /// <inheritdoc/>
    public bool Remove(T item)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIf(IsCompleted, ModificationAfterCompletionError);

        ImmutableList<T>? oldList, currentList = _items;
        do
        {
            oldList = currentList;
            ImmutableList<T> newList = oldList.Remove(item, _comparer);
            if (ReferenceEquals(newList, oldList))
            {
                return false;
            }
            currentList = Interlocked.CompareExchange(ref _items, newList, oldList);
        }
        while (!ReferenceEquals(currentList, oldList));

        RaiseCollectionChanged(item, ItemChangeType.Removed);

        return true;
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        ThrowHelpers.ThrowIf(IsCompleted, ModificationAfterCompletionError);

        ImmutableList<T>? oldList, currentList = _items;
        do
        {
            oldList = currentList;
            ThrowHelpers.ThrowIfOutOfRange(index, oldList.Count);
            ImmutableList<T> newList = oldList.RemoveAt(index);
            currentList = Interlocked.CompareExchange(ref _items, newList, oldList);
        }
        while (!ReferenceEquals(currentList, oldList));

        RaiseCollectionChanged(currentList[index], ItemChangeType.Removed);
    }

    /// <inheritdoc/>
    public override bool Contains(T item)
    {
        return _items.Contains(item);
    }

    /// <inheritdoc/>
    public int IndexOf(T item)
    {
        return _items.IndexOf(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        ThrowHelpers.ThrowIf(IsCompleted, ModificationAfterCompletionError);
        ImmutableList<T> oldList = Interlocked.Exchange(ref _items, []);
        using ImmutableList<T>.Enumerator enumerator = oldList.GetEnumerator();
        while (enumerator.MoveNext())
        {
            RaiseCollectionChanged(enumerator.Current, ItemChangeType.Removed);
        }
    }

    /// <inheritdoc/>
    public override int CopyTo(T[] array, int arrayIndex)
    {
        ImmutableList<T> items = _items;
        items.CopyTo(array, arrayIndex);
        return items.Count;
    }

    /// <inheritdoc/>
    public void Insert(int index, T item)
    {
        ImmutableList<T>? oldList, currentList = _items;
        do
        {
            oldList = currentList;
            ThrowHelpers.ThrowIfOutOfRange(index, oldList.Count);
            ImmutableList<T> newList = oldList.Insert(index, item);
            currentList = Interlocked.CompareExchange(ref _items, newList, oldList);
        }
        while (!ReferenceEquals(currentList, oldList));
        RaiseCollectionChanged(item, ItemChangeType.Added);
    }

    /// <inheritdoc/>
    public override ImmutableList<T>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }
}