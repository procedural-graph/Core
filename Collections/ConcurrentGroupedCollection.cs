using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace ProceduralGraph.Collections;

/// <summary>
/// Represents a thread-safe collection that groups items by a specified key and supports concurrent operations for
/// adding, removing, and enumerating items.
/// </summary>
/// <typeparam name="TKey">The type of the key used to group items. Must be non-nullable.</typeparam>
/// <typeparam name="TItem">The type of items stored in the collection.</typeparam>
public sealed class ConcurrentGroupedCollection<TKey, TItem> : 
    ConcurrentCollection<TItem, ConcurrentGroupedCollection<TKey, TItem>.Enumerator>,
    IReadOnlyDictionary<object, ImmutableHashSet<TItem>>,
    ICollection<TItem> 
    where TKey : class
{
    /// <summary>
    /// Enumerates the elements contained within a collection of immutable hash sets.
    /// </summary>
    public new struct Enumerator : IEnumerator<TItem>
    {
        private readonly IEnumerator<ImmutableHashSet<TItem>> _setEnumerator;
        private ImmutableHashSet<TItem>.Enumerator _itemEnumerator;
        private bool _initialized;
    
        internal Enumerator(IEnumerator<ImmutableHashSet<TItem>> setEnumerator)
        {
            _setEnumerator = setEnumerator;
            _itemEnumerator = default;
            _initialized = false;
        }

        /// <inheritdoc/>
        public readonly TItem Current => _itemEnumerator.Current;
        readonly object IEnumerator.Current => Current!;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_initialized && _itemEnumerator.MoveNext())
            {
                return true;
            }

            while (_setEnumerator.MoveNext())
            {
                _itemEnumerator = _setEnumerator.Current.GetEnumerator();
                _initialized = true;

                if (_itemEnumerator.MoveNext())
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _setEnumerator.Reset();
            _itemEnumerator = default;
            _initialized = false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _setEnumerator.Dispose();
            _itemEnumerator.Dispose();
        }
    }

    private readonly Func<TItem, TKey?> _keySelector;
    private readonly ConcurrentDictionary<object, ImmutableHashSet<TItem>> _items;

    private int _count;
    /// <inheritdoc/>
    public override int Count => _count;

    /// <inheritdoc/>
    protected override ILogger Logger { get; }

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys => _items.Keys.OfType<TKey>();
    IEnumerable<object> IReadOnlyDictionary<object, ImmutableHashSet<TItem>>.Keys => _items.Keys;

    IEnumerable<ImmutableHashSet<TItem>> IReadOnlyDictionary<object, ImmutableHashSet<TItem>>.Values => _items.Values;

    bool ICollection<TItem>.IsReadOnly => false;

    ImmutableHashSet<TItem> IReadOnlyDictionary<object, ImmutableHashSet<TItem>>.this[object key] => _items[key];

    /// <inheritdoc/>
    public ImmutableHashSet<TItem> this[TKey? key] => _items[key ?? _defaultKey];

    private readonly static object _defaultKey = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentGroupedCollection{TKey, TItem}"/> class using the specified key selector
    /// function.
    /// </summary>
    /// <param name="keySelector">A function that extracts the grouping key from each item. Cannot be <see langword="null"/>.</param>
    /// <param name="logger">The logger instance used to record diagnostic and operational messages. Cannot be <see langword="null"/>.</param>
    public ConcurrentGroupedCollection(Func<TItem, TKey?> keySelector, ILogger logger)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _items = new ConcurrentDictionary<object, ImmutableHashSet<TItem>>();
    }

    /// <summary>
    /// Attempts to add the specified item to the collection.
    /// </summary>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the item was successfully added; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool Add(TItem item)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(item);

        object key = _keySelector(item) ?? _defaultKey;
        ImmutableHashSet<TItem>? currentValue, newValue;
        do
        {
            currentValue = _items.GetOrAdd(key, []);
            newValue = currentValue.Add(item);
            if (ReferenceEquals(newValue, currentValue))
            {
                return false;
            }
        }
        while (_items.TryUpdate(key, newValue, currentValue));
        Interlocked.Increment(ref _count);

        RaiseCollectionChanged(item, ItemChangeType.Added);

        return true;
    }

    /// <summary>
    /// Removes the items associated with the specified key from the collection.
    /// </summary>
    /// <param name="key">The key whose associated items are to be removed from the collection.</param>
    /// <param name="items">
    /// hen this method returns, contains a read-only set of items that were removed if the key was found; otherwise,
    /// an empty set.
    /// </param>
    /// <returns><see langword="true"/> if the key was found and its items were removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool Remove(TKey? key, out ImmutableHashSet<TItem> items)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);

        if (_items.TryRemove(key ?? _defaultKey, out ImmutableHashSet<TItem>? value))
        {
            items = value;

            foreach (TItem item in value)
            {
                Interlocked.Decrement(ref _count);
                RaiseCollectionChanged(item, ItemChangeType.Removed);
            }

            return true;
        }

        items = [];

        return false;
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">Thrown if the collection has been disposed.</exception>
    public bool Remove(TItem item)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(item);

        object key = _keySelector(item) ?? _defaultKey;

        ImmutableHashSet<TItem>? currentSet, computedSet;
        do
        {
            if (!_items.TryGetValue(key, out currentSet))
            {
                return false;
            }

            computedSet = currentSet.Remove(item);

            if (ReferenceEquals(computedSet, currentSet))
            {
                return false;
            }
        }
        while (!_items.TryUpdate(key, computedSet, currentSet));

        if (computedSet.IsEmpty)
        {
            var kvp = new KeyValuePair<object, ImmutableHashSet<TItem>>(key, computedSet);
            return ((ICollection<KeyValuePair<object, ImmutableHashSet<TItem>>>)_items).Remove(kvp);
        }

        Interlocked.Decrement(ref _count);

        RaiseCollectionChanged(item, ItemChangeType.Removed);

        return true;
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey? key, out ImmutableHashSet<TItem> value)
    {
        if (_items.TryGetValue(key ?? _defaultKey, out ImmutableHashSet<TItem>? result))
        {
            value = result;
            return true;
        }

        value = [];
        return false;
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey? key)
    {
        return _items.ContainsKey(key ?? _defaultKey);
    }

    /// <inheritdoc/>
    public override bool Contains(TItem item)
    {
        ThrowHelpers.ThrowIfNull(item);

        object key = _keySelector(item) ?? _defaultKey;

        if (_items.TryGetValue(key, out ImmutableHashSet<TItem>? items))
        {
            return items.Contains(item);
        }

        return false;
    }

    /// <inheritdoc/>
    public override int CopyTo(TItem[] array, int arrayIndex)
    {
        ThrowHelpers.ThrowIfNull(array, nameof(array));
        ThrowHelpers.ThrowIfOutOfRange(arrayIndex, array.Length);
        if ((array.Length - arrayIndex) < Count)
        {
            throw new ArgumentException(
                $"The number of elements in the source collection is greater than the available space from {arrayIndex} to the end of the destination array.", 
                nameof(array));
        }
        using Enumerator enumerator = GetEnumerator();
        int i = arrayIndex;
        while (enumerator.MoveNext())
        {
            array[i++] = enumerator.Current;
        }
        return i;
    }

    /// <inheritdoc/>
    public override Enumerator GetEnumerator()
    {
        return new Enumerator(_items.Values.GetEnumerator());
    }

    void ICollection<TItem>.Add(TItem item)
    {
        Add(item);
    }

    void ICollection<TItem>.Clear()
    {
        foreach (var key in _items.Keys)
        {
            if (!_items.TryRemove(key, out ImmutableHashSet<TItem>? items) || items.IsEmpty)
            {
                continue;
            }

            using ImmutableHashSet<TItem>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Interlocked.Decrement(ref _count);
                RaiseCollectionChanged(enumerator.Current, ItemChangeType.Removed);
            }
        }
    }

    IEnumerator<KeyValuePair<object, ImmutableHashSet<TItem>>> IEnumerable<KeyValuePair<object, ImmutableHashSet<TItem>>>.GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    bool IReadOnlyDictionary<object, ImmutableHashSet<TItem>>.ContainsKey(object key)
    {
        return _items.ContainsKey(key);
    }

    bool IReadOnlyDictionary<object, ImmutableHashSet<TItem>>.TryGetValue(object key, [NotNull] out ImmutableHashSet<TItem>? value)
    {
        if (_items.TryGetValue(key, out value))
        {
            return true;
        }

        value = [];
        return false;
    }
}
