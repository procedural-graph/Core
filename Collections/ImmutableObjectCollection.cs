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

using _Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace ProceduralGraph.Collections;

/// <summary>
/// Represents an immutable collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
public sealed class ImmutableObjectCollection : ICollection<object>, IReadOnlyCollection<object>, IServiceProvider
{
    /// <summary>
    /// Represents a filtered query over an immutable collection of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to filter by.</typeparam>
    public readonly struct Query<T> : IEnumerable<T> where T : class
    {
        private readonly ImmutableObjectCollection _collection;

        internal Query(ImmutableObjectCollection collection)
        {
            _collection = collection;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(_collection);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_collection, typeof(T));
        }
    }

    /// <inheritdoc cref="Query{T}"/>
    public readonly struct Query : IEnumerable
    {
        private readonly ImmutableObjectCollection _collection;

        /// <summary>
        /// Gets the type filter for this query.
        /// </summary>
        public Type Filter { get; }

        internal Query(ImmutableObjectCollection collection, Type filter)
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

    /// <summary>
    /// Iterates over the elements of an immutable object collection, providing a strongly-typed enumerator
    /// for use with collection traversal constructs.
    /// </summary>
    /// <remarks>Yields objects of the same type before ones derived from it, in no particular order.</remarks>
    /// <typeparam name="T">The type of objects to enumerate.</typeparam>
    public struct Enumerator<T> : IEnumerator<T> where T : class
    {
        private Enumerator _enumerator;

        internal Enumerator(ImmutableObjectCollection collection)
        {
            _enumerator = new Enumerator(collection, typeof(T));
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

    /// <inheritdoc cref="Enumerator{T}"/>
    public struct Enumerator : IEnumerator
    {
        private readonly Type _type;
        private readonly ImmutableArray<object> _items;
        private readonly ImmutableArray<Index> _indices;
        private ReadOnlyMemory<int> _ids;
        private int _order, _low, _index, _position, _start, _end;
        private ClusterEnumerator _clusterEnumerator;

        internal Enumerator(ImmutableObjectCollection collection, Type type)
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

                int clusterStart = _indices[_position].StartIndex;
                int clusterEnd = ++_position < _indices.Length ? _indices[_position].StartIndex : _items.Length;
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

            ReadOnlySpan<Index> indices = _indices.AsSpan();
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
            ReadOnlySpan<Index> indices = _indices.AsSpan(_start, _end - _start);
            return indices.IndexOfSorted(_ids.Span[_index]);
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

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Index(int TypeID, int StartIndex) : IComparable<int>
    {
        public int CompareTo(int other)
        {
            return TypeID.CompareTo(other);
        }
    }

    private readonly ImmutableArray<Index> _itemIndices;

    private readonly ImmutableArray<object> _items;

#if NETCOREAPP3_0_OR_GREATER
    private static readonly Vector<int> _altMask;

    static ImmutableObjectCollection()
    {
        if (!Vector.IsHardwareAccelerated)
        {
            return;
        }

        Span<int> mask = stackalloc int[Vector<int>.Count];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = i % 2;
        }

        _altMask = new Vector<int>(mask);
    }
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableObjectCollection"/> class with no items.
    /// </summary>
    public ImmutableObjectCollection()
    {
        _items = [];
        _itemIndices = [];
    }

    private ImmutableObjectCollection(ImmutableArray<Index> itemIndices, ImmutableArray<object> items)
    {
        _itemIndices = itemIndices;
        _items = items;
    }

    /// <inheritdoc/>
    public int Count => _items.Length;

    bool ICollection<object>.IsReadOnly => true;

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

        ReadOnlySpan<Index> indices = _itemIndices.AsSpan();
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
                Index entry = indices[start + pos];
                result = _items[entry.StartIndex];
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

    /// <inheritdoc/>
    public bool Contains([NotNullWhen(true)] object? item)
    {
        if (_items.IsDefaultOrEmpty || item is null)
        {
            return false;
        }

        TypeRegistration registration = GlobalTypeRegistry.Get(item.GetType());
        ReadOnlySpan<Index> indices = _itemIndices.AsSpan();
        int index = indices.IndexOfSorted(registration.ID);

        if (index < 0)
        {
            return false;
        }

        int start = indices[index].StartIndex;
        int length = (++index < indices.Length ? indices[index].StartIndex : _items.Length) - start;
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
    public ImmutableObjectCollection Add<T>(T item) where T : class
    {
        return Add(item, typeof(T));
    }

    /// <summary>
    /// Returns a new collection with an specified item of the specified type added.
    /// </summary>
    /// <param name="type">The type of the item to add.</param>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public ImmutableObjectCollection Add(object item, Type type)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(type);

        Span<Index> indices = RentedArray.Copy(_itemIndices, out Index[]? indicesArray);
        Span<object> items = RentedArray.Copy(_items, out object[]? itemsArray);
        try
        {
            TypeRegistration registration = GlobalTypeRegistry.Get(type);
            int index = indices.IndexOfSorted(registration.ID), start;
            Index entry;

            if (index < 0)
            {
                index = ~index;
                indices = RentedArray.Grow(ref indicesArray, indices.Length + 1);
                indices[index..].CopyTo(indices[(index + 1)..]);
                start = items.Length;
                entry = new Index(registration.ID, start);
                indices[index] = entry;
            }
            else
            {
                entry = indices[index];
                start = entry.StartIndex;
                int adjacent = index + 1;
                int end = adjacent < indices.Length ? indices[adjacent].StartIndex : items.Length;
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

            OffsetIndices(indices[index..], 1);

            return new ImmutableObjectCollection([.. indices], [.. items]);
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
    public ImmutableObjectCollection Remove<T>(T item) where T : class
    {
        return Remove(item, typeof(T));
    }

    /// <summary>
    /// Returns a new collection with all instances of the specified item removed from the collection.
    /// </summary>
    /// <param name="type">The type of the item to remove.</param>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either the <paramref name="item"/> or the <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public ImmutableObjectCollection Remove(object item, Type type)
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(type);

        Span<Index> indices = RentedArray.Copy(_itemIndices, out Index[]? indicesArray);
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

                Index entry = indices[position];
                int start = adjacent >= 0 ? indices[adjacent].StartIndex : 0;
                int end = entry.StartIndex;

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
                OffsetIndices(indices[position..], -remaining);

                position = adjacent;
            }

            return modified ? new ImmutableObjectCollection([.. indices], [.. items]) : this;
        }
        finally
        {
            RentedArray.Return(ref indicesArray);
            RentedArray.Return(ref itemsArray);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc cref="AsSpan(int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<object> AsSpan() => _items.AsSpan();

    /// <inheritdoc cref="AsSpan(int, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<object> AsSpan(int start) => _items.AsSpan(start, _items.Length - start);

    /// <summary>
    /// Creates a new read-only span over the items in this collection.
    /// </summary>
    /// <param name="start">The zero-based index of the first item in the span.</param>
    /// <param name="length">The number of items in the span.</param>
    /// <returns>The read-only span representation of this collection.</returns>
    public ReadOnlySpan<object> AsSpan(int start, int length) => _items.AsSpan(start, length);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<object>.Enumerator GetEnumerator() => _items.GetEnumerator();

    private static void OffsetIndices(Span<Index> indices, int count)
    {
        int index = 0, length = indices.Length;

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            int indicesPerVector = Vector<int>.Count / 2;
            Vector<int> offset = _altMask * count;
            for (; (length - index) >= indicesPerVector; index += indicesPerVector)
            {
                _Unsafe.As<Index, Vector<int>>(ref indices[index]) += offset;
            }
        }
#endif

        for (; index < length; index++)
        {
            ref Index newEntry = ref indices[index];
            newEntry = newEntry with { StartIndex = newEntry.StartIndex + count };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cast<T>(object obj) where T : class
    {
#if DEBUG
        return (T)obj;
#else
        return _Unsafe.As<T>(obj);
#endif
    }

    private IEnumerator<object> GetEnumeratorImpl()
    {
        foreach (object item in _items)
        {
            yield return item;
        }
    }

    void ICollection<object>.Add(object item)
    {
        ThrowHelpers.ThrowNotSupportedException(this);
    }

    void ICollection<object>.Clear()
    {
        ThrowHelpers.ThrowNotSupportedException(this);
    }

    bool ICollection<object>.Remove(object item)
    {
        ThrowHelpers.ThrowNotSupportedException(this);
        return false;
    }

    object? IServiceProvider.GetService(Type serviceType)
    {
        TryGetOne(serviceType, out object? result);
        return result;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }

    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }
}
