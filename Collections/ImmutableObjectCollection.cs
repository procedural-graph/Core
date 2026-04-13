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
public sealed class ImmutableObjectCollection : IReadOnlyCollection<object>
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct Index(int TypeID, int StartIndex) : IComparable, IComparable<Index>
    {
        public int CompareTo(object? obj)
        {
            return obj is Index other ? CompareTo(other) : 1;
        }

        public int CompareTo(Index other)
        {
            return TypeID.CompareTo(other.TypeID);
        }
    }

    private const int BinarySearchThreshold = 16;

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

    /// <summary>
    /// Attempts to retrieve a single instance of type <typeparamref name="T"/> from the collection.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve. Must be a reference type.</typeparam>
    /// <param name="result">
    /// When this method returns, contains the first instance of <typeparamref name="T"/> if one is found; otherwise, 
    /// <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if an object of type <typeparamref name="T"/> was found and assigned to result; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetOne<T>([NotNullWhen(true)] out T? result) where T : class
    {
        if (_items.IsDefaultOrEmpty)
        {
            result = null;
            return false;
        }

        ReadOnlySpan<Index> indices = _itemIndices.AsSpan();
        (int _, int order, ImmutableArray<int> assignableTo) = GlobalTypeRegistry.Get<T>();
        ReadOnlySpan<int> ids = assignableTo.AsSpan();
        return TryGetOne(indices, ids[order..], out result, out int low) || TryGetOne(indices[..low], ids[..order], out result, out _);
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
    /// Retrieves all items of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// A read-only span containing all contiguous items of type <typeparamref name="T"/> 
    /// found in the collection; or <see cref="ReadOnlySpan{T}.Empty"/> if no such items are present.
    /// </returns>
    public IEnumerable<T> GetMany<T>() where T : class
    {
        (int _, int order, ImmutableArray<int> derivedTypes) = GlobalTypeRegistry.Get<T>();
        ReadOnlySpan<int> ids = derivedTypes.AsSpan();
        ReadOnlySpan<Index> indices = _itemIndices.AsSpan();
        ReadOnlySpan<object> items = _items.AsSpan();
        int low = IndexOf(indices, derivedTypes[order]), position = low, offset = 0, index = order + 1;
        for (int i = 0; i < 2; i++)
        {
            do
            {
                if (position >= 0)
                {
                    int start = indices[offset += position].StartIndex;
                    int end = ++offset < indices.Length ? indices[offset].StartIndex : _items.Length;

                    foreach (object obj in items[start..end])
                    {
                        yield return Cast<T>(obj);
                    }
                }
                else
                {
                    offset += ~position;
                }

                position = IndexOf(indices[offset..], ids[index]);
            }
            while (++index < ids.Length);

            ids = ids[..order];
            if (ids.IsEmpty)
            {
                break;
            }
            indices = indices[..(low >= 0 ? low : ~low)];
            position = IndexOf(indices, ids[0]);
            offset = 0;
            index = 1;
        }
    }

    /// <inheritdoc cref="Add{T}(T, IEqualityComparer{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableObjectCollection Add<T>(T item) where T : class
    {
        return Add(item, EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Returns a new collection with the specified item of type <typeparamref name="T"/> added.
    /// </summary>
    /// <typeparam name="T">The type of the item to add.</typeparam>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <param name="comparer">An object that determines whether two instances of <typeparamref name="T"/> are equal.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    public ImmutableObjectCollection Add<T>(T item, IEqualityComparer<T> comparer) where T : class
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(comparer);

        Span<Index> indices = RentedArray.Copy(_itemIndices, out Index[]? indicesArray);
        Span<object> items = RentedArray.Copy(_items, out object[]? itemsArray);
        try
        {
            TypeRegistration registration = GlobalTypeRegistry.Get<T>();
            int index = IndexOf(indices, registration.ID), start;
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
                    T candidate = _Unsafe.As<T>(obj);
                    if (comparer.Equals(candidate, item))
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

    /// <inheritdoc cref="Remove{T}(T, IEqualityComparer{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableObjectCollection Remove<T>(T item) where T : class
    {
        return Remove(item, EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Returns a new collection with all instances of the specified item removed from the collection.
    /// </summary>
    /// <typeparam name="T">The type of the item to remove.</typeparam>
    /// <param name="item">The item to remove from the collection.</param>
    /// <param name="comparer">An object that determines whether two instances of <typeparamref name="T"/> are equal.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    public ImmutableObjectCollection Remove<T>(T item, IEqualityComparer<T> comparer) where T : class
    {
        ThrowHelpers.ThrowIfNull(item);
        ThrowHelpers.ThrowIfNull(comparer);

        Span<Index> indices = RentedArray.Copy(_itemIndices, out Index[]? indicesArray);
        Span<object> items = RentedArray.Copy(_items, out object[]? itemsArray);

        try
        {
            bool modified = false;

            (int _, int _, ImmutableArray<int> derivedTypes) = GlobalTypeRegistry.Get<T>();
            int position = indices.Length;
            for (int i = derivedTypes.Length - 1; i >= 0; i--)
            {
                position = IndexOf(indices[..position], derivedTypes[i]);
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
                    if (!comparer.Equals(Cast<T>(cluster[j]), item))
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

    /// <summary>
    /// Creates a new read-only span over the items in this collection.
    /// </summary>
    /// <returns>The read-only span representation of this collection.</returns>
    public ReadOnlySpan<object> AsSpan() => _items.AsSpan();

    /// <summary>
    /// Creates a new read-only span over the items in this collection.
    /// </summary>
    /// <param name="start">The zero-based index of the first item in the span.</param>
    /// <param name="length">The number of items in the span.</param>
    /// <returns>The read-only span representation of this collection.</returns>
    public ReadOnlySpan<object> AsSpan(int start, int length) => _items.AsSpan(start, length);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public ImmutableArray<object>.Enumerator GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    private bool TryGetOne<T>(ReadOnlySpan<Index> indices, ReadOnlySpan<int> ids, out T? result, out int low) where T : class
    {
        result = null;

        int position = IndexOf(indices, ids[0]), index = 1, offset = 0;
        low = position;

        do
        {
            if (position >= 0)
            {
                Index entry = indices[offset + position];
                result = Cast<T>(_items[entry.StartIndex]);
                break;
            }

            offset += ~position;
            position = IndexOf(indices[offset..], ids[index++]);
        }
        while (index < ids.Length);

        low = low >= 0 ? low : ~low;

        return result is { };
    }

    private static int IndexOf(ReadOnlySpan<Index> indices, int id)
    {
        int length = indices.Length;

        if (length < BinarySearchThreshold)
        {
            for (int i = 0; i < length; i++)
            {
                switch (indices[i].TypeID)
                {
                    case int value when value > id: return ~i;
                    case int value when value == id: return i;
                }
            }

            return ~length;
        }

        int low = 0, high = length - 1;
        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            switch (indices[mid].TypeID)
            {
                case int value when value == id: return mid;
                case int value when value < id: low = mid + 1; break;
                default: high = mid - 1; break;
            }
        }

        return ~low;
    }

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

    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }
}
