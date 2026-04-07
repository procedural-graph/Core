using System;
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

    /// <inheritdoc/>
    protected override ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    /// <param name="comparer">The equality comparer to use for comparing items in the list. 
    /// This parameter cannot be <see langword="null"/>.
    /// </param>
    /// <param name="logger">The logger instance used to record diagnostic and operational messages. Cannot be <see langword="null"/>.</param>
    public ConcurrentList(IEnumerable<T> collection, IEqualityComparer<T> comparer, ILogger logger)
    {
        ThrowHelpers.ThrowIfNull(collection);
        _items = [.. collection];

        ThrowHelpers.ThrowIfNull(comparer);
        _comparer = comparer;

        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="ConcurrentList{T}.ConcurrentList(IEnumerable{T}, IEqualityComparer{T}, ILogger)"/>
    public ConcurrentList(IEnumerable<T> collection, ILogger logger)
    {
        ThrowHelpers.ThrowIfNull(collection);
        _items = [.. collection];

        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _comparer = EqualityComparer<T>.Default;
    }

    /// <inheritdoc cref="ConcurrentList{T}.ConcurrentList(IEnumerable{T}, IEqualityComparer{T}, ILogger)"/>
    public ConcurrentList(IEqualityComparer<T> comparer, ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ThrowHelpers.ThrowIfNull(comparer);
        _comparer = comparer;
        _items = [];
    }

    /// <inheritdoc cref="ConcurrentList{T}.ConcurrentList(IEnumerable{T}, IEqualityComparer{T}, ILogger)"/>
    public ConcurrentList(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _comparer = EqualityComparer<T>.Default;
        _items = [];
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
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
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public void Add(T item)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfDisposed(Disposed, this);

        if (ImmutableInterlocked.Update(ref _items, static (l, i) => l.Add(i), item))
        {
            RaiseCollectionChanged(item, ItemChangeType.Added);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool Remove(T item)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfDisposed(Disposed, this);

        if (ImmutableInterlocked.Update(ref _items, static (l, i) => l.Remove(i), item))
        {
            RaiseCollectionChanged(item, ItemChangeType.Removed);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public void RemoveAt(int index)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
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
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public override bool Contains(T item)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return _items.Contains(item);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public int IndexOf(T item)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return _items.IndexOf(item);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public void Clear()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ImmutableList<T> oldList = Interlocked.Exchange(ref _items, []);
        foreach (T item in oldList)
        {
            RaiseCollectionChanged(item, ItemChangeType.Removed);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public override int CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ImmutableList<T> items = _items;
        items.CopyTo(array, arrayIndex);
        return items.Count;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public void Insert(int index, T item)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        if (ImmutableInterlocked.Update(ref _items, Insert, new KeyValuePair<int, T>(index, item)))
        {
            RaiseCollectionChanged(item, ItemChangeType.Added);
        }
    }

    private ImmutableList<T> Insert(ImmutableList<T> list, KeyValuePair<int, T> pair)
    {
        ThrowHelpers.ThrowIfOutOfRange(pair.Key, list.Count);
        return list.Insert(pair.Key, pair.Value);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public override ImmutableList<T>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }
}