using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif

namespace ProceduralGraph;

/// <summary>
/// Represents an immutable collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
public sealed class Repository : ICollection<KeyValuePair<Type, object>>, IReadOnlyDictionary<Type, object>, IServiceProvider
{
    /// <summary>
    /// Represents a filtered query over an immutable collection of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to filter by.</typeparam>
    public readonly struct Query<T> : IEnumerable<T> where T : class
    {
        /// <summary>
        /// Iterates over the elements of an immutable object collection, providing a strongly-typed enumerator
        /// for use with collection traversal constructs.
        /// </summary>
        /// <remarks>Yields objects of the same type before ones derived from it, in no particular order.</remarks>
        public struct Enumerator : IEnumerator<T>
        {
            private Query.Enumerator _enumerator;

            internal Enumerator(Repository collection)
            {
                _enumerator = new Query.Enumerator(collection, typeof(T));
            }

            /// <inheritdoc/>
            public readonly T Current => Cast<T>(_enumerator.Current);
            readonly object IEnumerator.Current => _enumerator.Current;

            /// <inheritdoc/>
            public bool MoveNext() => _enumerator.MoveNext();

            /// <inheritdoc/>
            public void Reset() => _enumerator.Reset();

            readonly void IDisposable.Dispose() { }
        }

        private readonly Repository _collection;

        internal Query(Repository collection)
        {
            _collection = collection;
        }

        /// <inheritdoc/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_collection);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <inheritdoc cref="Query{T}"/>
    public readonly struct Query : IEnumerable
    {
        /// <inheritdoc cref="Query{T}.Enumerator"/>
        public struct Enumerator : IEnumerator
        {
            private readonly Type _type;
            private readonly ImmutableArray<object> _items;
            private readonly ImmutableArray<IntegerLookup> _indices;
            private ReadOnlyMemory<int> _ids;
            private int _order, _low, _index, _position, _start, _end;
            private ClusterEnumerator _clusterEnumerator;

            internal Enumerator(Repository collection, Type type)
            {
                _type = type;
                _items = collection._items;
                _indices = collection._itemIndices;
                Reset();
                Current = default!;
            }

            /// <inheritdoc/>
            public object Current { get; private set; }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                while (!_clusterEnumerator.MoveNext())
                {
                    while (_position < 0)
                    {
                        if (++_index > _ids.Length)
                        {
                            _index = -1;
                            (_end, _low) = (_low, 0);
                            _start = 0;
                            _ids = _ids[.._order];
                            _order = 0;
                        }

                        if (_ids.IsEmpty)
                        {
                            return false;
                        }

                        _start += ~_position;
                        _position = AdvancePosition();
                    }

                    int clusterStart = _indices[_position].Index;
                    int clusterEnd = ++_position < _indices.Length ? _indices[_position].Index : _items.Length;
                    _clusterEnumerator = new ClusterEnumerator(_items, clusterStart, clusterEnd);
                }

                Current = _clusterEnumerator.Current;

                _start += _position;
                _position = AdvancePosition();

                return true;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                (int _, _order, ImmutableArray<int> derived) = GlobalTypeRegistry.Get(_type);
                _ids = derived.AsMemory();

                ReadOnlySpan<IntegerLookup> indices = _indices.AsSpan();
                _low = indices.IndexOfSorted(derived[_order]);
                _low = _low >= 0 ? _low : ~_low;
                _position = _low;

                _index = _order - 1;

                _end = _indices.Length;

                _clusterEnumerator = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private readonly int AdvancePosition()
            {
                ReadOnlySpan<IntegerLookup> indices = _indices.AsSpan(_start, _end - _start);
                return indices.IndexOfSorted(_ids.Span[_index]);
            }
        }

        private readonly Repository _collection;

        /// <summary>
        /// Gets the type filter for this query.
        /// </summary>
        public Type Filter { get; }

        internal Query(Repository collection, Type filter)
        {
            _collection = collection;
            Filter = filter;
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_collection, Filter);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <inheritdoc cref="IEnumerator{T}"/>
    public struct Enumerator : IEnumerator<KeyValuePair<Type, object>>
    {
        private readonly ImmutableArray<IntegerLookup> _indices;
        private readonly ImmutableArray<object> _items;
        private ClusterEnumerator _enumerator;
        private Type[]? _lookup;
        private int _index;

        /// <inheritdoc/>
        public KeyValuePair<Type, object> Current { readonly get; private set; }
        readonly object IEnumerator.Current => Current;

        internal Enumerator(Repository collection)
        {
            _indices = collection._itemIndices;
            _items = collection._items;
            _lookup = GlobalTypeRegistry.LeaseReverseLookup();
            _index = -1;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            while (!_enumerator.MoveNext())
            {
                if (++_index >= _indices.Length)
                {
                    return false;
                }

                int start = _indices[_index].Index;
                int end = ++_index < _indices.Length ? _indices[_index].Index : _items.Length;
                _enumerator = new ClusterEnumerator(_items, start, end);
            }

            IntegerLookup entry = _indices[_index];
            Current = new KeyValuePair<Type, object>(_lookup![entry.Key], _enumerator.Current);
            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            RentedArray.TryReturn(ref _lookup);
        }

        readonly void IEnumerator.Reset()
        {
            ThrowHelpers.ThrowNotSupportedException(this);
        }
    }

    /// <summary>
    /// Represents a read-only collection of all type keys contained in the associated repository.
    /// </summary>
    public readonly struct KeyCollection : IEnumerable<Type>
    {
        /// <inheritdoc cref="IEnumerator{T}"/>
        public struct Enumerator : IEnumerator<Type>
        {
            private readonly ImmutableArray<IntegerLookup> _indices;
            private Type[]? _lookup;
            private int _index;

            /// <inheritdoc/>
            public Type Current { readonly get; private set; }
            readonly object IEnumerator.Current => Current;

            internal Enumerator(KeyCollection collection)
            {
                _index = -1;
                _indices = collection._repository._itemIndices;
                _lookup = GlobalTypeRegistry.LeaseReverseLookup();
                Current = default!;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (++_index >= _indices.Length)
                {
                    return false;
                }

                IntegerLookup entry = _indices[_index];
                Current = _lookup![entry.Key];

                return true;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                RentedArray.TryReturn(ref _lookup);
            }

            readonly void IEnumerator.Reset()
            {
                ThrowHelpers.ThrowNotSupportedException(this);
            }
        }

        private readonly Repository _repository;

        internal KeyCollection(Repository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private struct ClusterEnumerator(ImmutableArray<object> items, int start, int end) : IEnumerator
    {
        private readonly ImmutableArray<object> _items = items;
        private readonly int _start = start;
        private readonly int _end = end;
        private int _index = start - 1;

        public object Current { readonly get; private set; } = default!;

        public bool MoveNext()
        {
            if (++_index >= _end)
            {
                return false;
            }

            Current = _items[_index];

            return true;
        }

        public void Reset() => _index = _start - 1;
    }

    private readonly ImmutableArray<IntegerLookup> _itemIndices;

    private readonly ImmutableArray<object> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository"/> class with no items.
    /// </summary>
    public Repository()
    {
        _items = [];
        _itemIndices = [];
    }

    private Repository(ImmutableArray<IntegerLookup> itemIndices, ImmutableArray<object> items)
    {
        _itemIndices = itemIndices;
        _items = items;
    }

    /// <inheritdoc/>
    public int Count => _items.Length;

    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => true;

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.Keys"/>
    public KeyCollection Keys => new(this);
    IEnumerable<Type> IReadOnlyDictionary<Type, object>.Keys => Keys;

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.Values"/>
    public ImmutableArray<object> Values => _items;
    IEnumerable<object> IReadOnlyDictionary<Type, object>.Values => _items;

    /// <inheritdoc/>
    public object this[Type key] => GetOne(key);

    /// <typeparam name="T">The type of object to retrieve. Must be a reference type.</typeparam>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne<T>([NotNullWhen(true)] out T? result) where T : class
    {
        if (TryGetOne(typeof(T), out object? obj))
        {
            result = Cast<T>(obj);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no object of the specified type is found.</exception>
    /// <inheritdoc cref="TryGetOne{T}(out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOne<T>() where T : class
    {
        ThrowHelpers.ThrowIf(!TryGetOne(out T? obj), $"Object of type {typeof(T).FullName} not found.");
        return obj;
    }

    /// <summary>
    /// Attempts to retrieve a single instance of the specified type from the collection.
    /// </summary>
    /// <param name="type">The type of object to retrieve.</param>
    /// <param name="result">
    /// When this method returns, contains the first instance of the specified type if one is found; otherwise, 
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an object of the specified type was found and assigned to <paramref name="result"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? result)
    {
        if (_items.IsDefaultOrEmpty || type is null)
        {
            result = null;
            return false;
        }

        ReadOnlySpan<IntegerLookup> indices = _itemIndices.AsSpan();
        (int _, int order, ImmutableArray<int> derived) = GlobalTypeRegistry.Get(type);
        ReadOnlySpan<int> ids = derived.AsSpan();

        int end = indices.Length;
        do
        {
            int start = 0, index = order, low = indices.IndexOfSorted(ids[index]), pos = low;
        Check:
            if (pos < 0)
            {
                start += ~pos;
                if (++index < ids.Length)
                {
                    pos = indices[start..end].IndexOfSorted(ids[index]);
                    goto Check;
                }
            }
            else
            {
                IntegerLookup entry = indices[start + pos];
                result = _items[entry.Index];
                return true;
            }
            end = low < 0 ? ~low : low;
            ids = ids[..order];
            order = 0;
        }
        while (!ids.IsEmpty);
        result = null;
        return false;
    }

    /// <summary>
    /// Retrieves the first object of the specified type from the collection.
    /// </summary>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <returns>The first object of the specified type found in the collection.</returns>
    /// <inheritdoc cref="TryGetOne(Type, out object)"/>
    /// <exception cref="InvalidOperationException">Thrown if no object of the specified type is found.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    public object GetOne(Type type)
    {
        ThrowHelpers.ThrowIfNull(type);
        ThrowHelpers.ThrowIf(!TryGetOne(type, out object? result), $"Object of type {type.FullName} not found.");
        return result;
    }

    /// <typeparam name="T">The type of object to check for. Must be a reference type.</typeparam>
    /// <inheritdoc cref="Contains(object, Type)"/>
    public bool Contains<T>(T item) where T : class
    {
        return Contains(item, typeof(T));
    }

    /// <summary>
    /// Determines whether the collection contains the specified item of the given type.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <param name="type">The type to use when searching for the item.</param>
    /// <returns>
    /// <see langword="true"/> if the item is found in the collection for the specified type; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(object item, Type type)
    {
        if (_items.IsDefaultOrEmpty || item is null)
        {
            return false;
        }

        TypeRegistration registration = GlobalTypeRegistry.Get(type ?? item.GetType());
        ReadOnlySpan<IntegerLookup> indices = _itemIndices.AsSpan();
        int index = indices.IndexOfSorted(registration.ID);

        if (index < 0)
        {
            return false;
        }

        int start = indices[index].Index;
        int length = (++index < indices.Length ? indices[index].Index : _items.Length) - start;
        ReadOnlySpan<object> items = _items.AsSpan(start, length);

        foreach (object obj in items)
        {
            if (Equals(obj, item))
            {
                return true;
            }
        }

        return false;
    }

    /// <typeparam name="T">The type of object to check for. Must be a reference type.</typeparam>
    /// <inheritdoc cref="Contains(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<T>() where T : class
    {
        return Contains(typeof(T));
    }

    /// <summary>
    /// Determines whether the collection contains a registration for the specified type.
    /// </summary>
    /// <param name="type">The type to locate in the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the collection contains a registration for the specified type; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(Type type)
    {
        if (_items.IsDefaultOrEmpty || type is null)
        {
            return false;
        }

        TypeRegistration registration = GlobalTypeRegistry.Get(type);
        ReadOnlySpan<IntegerLookup> indices = _itemIndices.AsSpan();

        return indices.IndexOfSorted(registration.ID) >= 0;
    }

    /// <param name="type">The type of items to retrieve.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="GetAll{T}()"/>
    public Query GetAll(Type type)
    {
        ThrowHelpers.ThrowIfNull(type);
        return new Query(this, type);
    }

    /// <summary>
    /// Retrieves all items of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// An enumerable sequence containing all contiguous items of the specified type found in the 
    /// collection; or an empty sequence if no such items are present.
    /// </returns>
    public Query<T> GetAll<T>() where T : class
    {
        return new Query<T>(this);
    }

    /// <typeparam name="T"> The type of the item to add. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Add(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Repository Add<T>(T item) where T : class
    {
        return Add(item, typeof(T));
    }

    /// <summary>
    /// Adds the specified item to the collection.
    /// </summary>
    /// <param name="type">The type of the item to add.</param>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// A new <see cref="Repository"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public Repository Add(object item, Type type)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(type);

        Span<IntegerLookup> indices = RentedArray.Copy(_itemIndices, out IntegerLookup[]? indicesArray);
        Span<object> items = RentedArray.Copy(_items, out object[]? itemsArray);
        try
        {
            TypeRegistration registration = GlobalTypeRegistry.Get(type);
            int index = indices.IndexOfSorted(registration.ID), start;
            IntegerLookup entry;

            if (index < 0)
            {
                index = ~index;
                indices = RentedArray.Grow(ref indicesArray, indices.Length + 1);
                indices[index..].CopyTo(indices[(index + 1)..]);
                start = items.Length;
                entry = new IntegerLookup(registration.ID, start);
                indices[index] = entry;
            }
            else
            {
                entry = indices[index];
                start = entry.Index;
                int adjacent = index + 1;
                int end = adjacent < indices.Length ? indices[adjacent].Index : items.Length;
                foreach (object obj in items[start..end])
                {
                    if (Equals(obj, item))
                    {
                        return this;
                    }
                }
            }

            items = RentedArray.Grow(ref itemsArray, items.Length + 1);
            items[start..^1].CopyTo(items[(start + 1)..]);
            items[start] = item;

            IntegerLookup.Offset(indices[index..], 1);

            return new Repository([.. indices], [.. items]);
        }
        finally
        {
            RentedArray.Return(ref indicesArray);
            RentedArray.Return(ref itemsArray);
        }
    }

    /// <typeparam name="T">The type of the item to remove. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Remove(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Repository Remove<T>(T item) where T : class
    {
        return Remove(item, typeof(T));
    }

    /// <summary>
    /// Removes the specified item from the collection.
    /// </summary>
    /// <param name="type">The type of the item to remove.</param>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>
    /// A new <see cref="Repository"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public Repository Remove(object item, Type type)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(type);

        Span<IntegerLookup> indices = RentedArray.Copy(_itemIndices, out IntegerLookup[]? indicesArray);
        Span<object> items = RentedArray.Copy(_items, out object[]? itemsArray);

        try
        {
            bool modified = false;

            (int _, int _, ImmutableArray<int> derived) = GlobalTypeRegistry.Get(type);
            int position = indices.Length;
            for (int i = derived.Length - 1; i >= 0; i--)
            {
                position = indices[..position].IndexOfSorted(derived[i]);
                int adjacent = position - 1;

                if (position < 0)
                {
                    position = ~position;
                    continue;
                }

                modified = true;

                IntegerLookup entry = indices[position];
                int start = adjacent >= 0 ? indices[adjacent].Index : 0;
                int end = entry.Index;

                Span<object> cluster = items[start..end];
                for (int j = cluster.Length - 1; j >= 0; j--)
                {
                    if (!Equals(cluster[j], item))
                    {
                        continue;
                    }

                    cluster[(j + 1)..].CopyTo(cluster[j..]);
                    cluster = cluster[..^1];
                }

                int length = cluster.Length, remaining = end - start - length;

                items[end..].CopyTo(items[(start + length)..]);
                items = items[..^length];

                if (length == 0)
                {
                    indices[position..].CopyTo(indices[adjacent..]);
                    indices = indices[..^1];
                }
                IntegerLookup.Offset(indices[position..], -remaining);

                position = adjacent;
            }

            return modified ? new Repository([.. indices], [.. items]) : this;
        }
        finally
        {
            RentedArray.Return(ref indicesArray);
            RentedArray.Return(ref itemsArray);
        }
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    /// Constructs a new instance of the <see cref="Repository"/> class from the specified key-value pairs.
    /// </summary>
    /// <param name="values">The key-value pairs representing the types and corresponding objects to include in the repository.</param>
    public static Repository FromRange(ReadOnlySpan<KeyValuePair<Type, object>> values)
    {
        if (values.IsEmpty)
        {
            return [];
        }

        int length = values.Length;
        object[] itemArray = new object[length];
        int[]? idArray = RentedArray.Acquire<int>(length);
        IntegerLookup[]? indicesArray = null;
        try
        {
            int index = 0;
            foreach (KeyValuePair<Type, object> entry in values)
            {
                ThrowHelpers.ThrowIfNull(entry.Key);
                ThrowHelpers.ThrowIfNull(entry.Value);
                TypeRegistration registration = GlobalTypeRegistry.Get(entry.Key);
                itemArray[index] = entry.Value;
                idArray[index++] = registration.ID;
            }
            Array.Sort(idArray, itemArray, 0, length);

            indicesArray = RentedArray.Acquire<IntegerLookup>(length);
            index = 0;
            int currentID = idArray[0], start = 0;
            for (int end = 1; end < length; end++)
            {
                ref readonly int id = ref idArray[end];
                if (id != currentID)
                {
                    indicesArray[index++] = new IntegerLookup(currentID, start);
                    currentID = id;
                    start = end;
                }
            }
            indicesArray[index] = new IntegerLookup(currentID, start);

            ImmutableArray<IntegerLookup> itemIndices = ImmutableArray.Create(indicesArray, 0, index + 1);
            ImmutableArray<object> items = ImmutableCollectionsMarshal.AsImmutableArray(itemArray);
            return new Repository(itemIndices, items);
        }
        finally
        {
            RentedArray.Return(ref idArray);
            RentedArray.TryReturn(ref indicesArray);
        }
    }

    /// <param name="start">The zero-based index of the first element in the specified range.</param>
    /// <param name="length">The number of elements in the specified range.</param>
    /// <inheritdoc cref="FromRange(ReadOnlySpan{KeyValuePair{Type, object}})"/>
    /// <param name="values"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Repository FromRange(KeyValuePair<Type, object>[] values, int start, int length)
    {
        Span<KeyValuePair<Type, object>> span = values.AsSpan(start, length);
        return FromRange(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cast<T>(object obj) where T : class
    {
#if DEBUG
        return (T)obj;
#else
        return Unsafe.As<T>(obj);
#endif
    }

    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item)
    {
        ThrowHelpers.ThrowNotSupportedException(this);
    }

    void ICollection<KeyValuePair<Type, object>>.Clear()
    {
        ThrowHelpers.ThrowNotSupportedException(this);
    }

    bool ICollection<KeyValuePair<Type, object>>.Contains(KeyValuePair<Type, object> item)
    {
        return Contains(item.Value, item.Key);
    }

    void ICollection<KeyValuePair<Type, object>>.CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
    {
        ThrowHelpers.ThrowIfOutOfRange(Count, arrayIndex, array);
        foreach (KeyValuePair<Type, object> entry in this)
        {
            array[arrayIndex++] = entry;
        }
    }

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item)
    {
        ThrowHelpers.ThrowNotSupportedException(this);
        return false;
    }

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    bool IReadOnlyDictionary<Type, object>.ContainsKey(Type key)
    {
        return Contains(key);
    }

    bool IReadOnlyDictionary<Type, object>.TryGetValue(Type key, out object value)
    {
        return TryGetOne(key, out value!);
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        TryGetOne(serviceType, out object? result);
        return result;
    }
}
