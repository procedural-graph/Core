using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
    private readonly ref struct TypeIdentity<TSelf>
    {
        public static int ID { get; } = NextID();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Index(int TypeID, int StartIndex);

    private static int _currentTypeID;

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
        if (TryGetIndex<T>(out _, out Index entry))
        {
            result = _Unsafe.As<T>(_items[entry.StartIndex]);
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
        ThrowHelpers.ThrowIf(!TryGetOne(out T? component), $"Object of type {typeof(T).FullName} not found.");
        return component;
    }

    /// <summary>
    /// Determines whether the collection contains an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the object to check for.</typeparam>
    /// <returns>
    /// <see langword="true"/> if the collection contains an object of the specified type; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<T>() where T : class
    {
        return TryGetOne<T>(out _);
    }

    /// <summary>
    /// Retrieves all items of the specified type from the collection.
    /// </summary>
    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>.NET Framework</term>
    /// <description>Allocates a new array to hold the items of type <typeparamref name="T"/>.</description>
    /// </item>
    /// <item>
    /// <term>.NET Core &amp; .NET Standard</term>
    /// <description>
    /// Creates a read-only span directly over the contiguous segment of items of type <typeparamref name="T"/> 
    /// in the collection, without any additional allocations.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// A read-only span containing all contiguous items of type <typeparamref name="T"/> 
    /// found in the collection; or <see cref="ReadOnlySpan{T}.Empty"/> if no such items are present.
    /// </returns>
    public ReadOnlySpan<T> GetMany<T>() where T : class
    {
        if (!TryGetIndex<T>(out _, out Index entry))
        {
            return [];
        }

        int start = entry.StartIndex, end = start;
        while (end < _items.Length && _items[end] is T)
        {
            end++;
        }

        ref object objRef = ref _Unsafe.AsRef(in _items.ItemRef(start));
        ref T itemRef = ref _Unsafe.As<object, T>(ref objRef);
#if NETFRAMEWORK
        T[] itemArray = new T[end - start];
        for (int i = 0; i < itemArray.Length; i++)
        {
            itemArray[i] = _Unsafe.Add(ref itemRef, i);
        }
        return itemArray;
#else
        return MemoryMarshal.CreateReadOnlySpan(ref itemRef, end - start);
#endif
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

        Index[] indicesArray;
        ReadOnlySpan<Index> srcIndices = _itemIndices.AsSpan();
        if (TryGetIndex<T>(out int index, out Index entry))
        {
            for (int i = entry.StartIndex; i < _items.Length && _items[i] is T currentItem; i++)
            {
                if (comparer.Equals(currentItem, item))
                {
                    return this;
                }
            }

            indicesArray = [.. srcIndices[..index], entry, .. srcIndices[index..]];
            OffsetIndices(indicesArray.AsSpan(index + 1), 1);
        }
        else
        {
            index = ~index;            
            indicesArray = [.. srcIndices[..index], entry, .. srcIndices[index..]];
        }

        ImmutableArray<Index> itemIDs = ImmutableCollectionsMarshal.AsImmutableArray(indicesArray);
        ImmutableArray<object> items = _items.Insert(entry.StartIndex, item);

        return new ImmutableObjectCollection(itemIDs, items);
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

        if (!TryGetIndex<T>(out int index, out Index entry))
        {
            return this;
        }

        ImmutableArray<object> items;
        int removedCount, keptCount = 0;

        object[]? itemsArray = RentedArray.Acquire<object>();
        try
        {
            int start = entry.StartIndex, end = start;
            bool modified = false;
            for (; end < _items.Length && _items[end] is T candidate; end++)
            {
                if (comparer.Equals(candidate, item))
                {
                    modified = true;
                    continue;
                }

                int i = keptCount++;
                RentedArray.Grow(ref itemsArray, keptCount);
                itemsArray[i] = candidate;
            }

            if (!modified)
            {
                return this;
            }

            ReadOnlySpan<object> currentItems = _items.AsSpan();
            items = [.. currentItems[..start], .. itemsArray.AsSpan(0, keptCount), .. currentItems[end..]];

            removedCount = end - start - keptCount;
        }
        finally
        {
            RentedArray.Return(ref itemsArray);
        }

        ReadOnlySpan<Index> srcIndices = _itemIndices.AsSpan();
        Index[] indicesArray = keptCount == 0 ? [.. srcIndices[..index], .. srcIndices[(index + 1)..]] : [.. srcIndices];
        OffsetIndices(indicesArray.AsSpan(index), -removedCount);
        ImmutableArray<Index> itemIDs = ImmutableCollectionsMarshal.AsImmutableArray(indicesArray);

        return new ImmutableObjectCollection(itemIDs, items);
    }

    /// <inheritdoc cref="Replace{T}(T, T, IEqualityComparer{T})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableObjectCollection Replace<T>(T oldItem, T newItem) where T : class
    {
        return Replace(oldItem, newItem, EqualityComparer<T>.Default);
    }

    /// <summary>
    /// Replaces all instances of <paramref name="oldItem"/> with <paramref name="newItem"/> in the collection.
    /// </summary>
    /// <typeparam name="T">The type of the items to replace. Must be a reference type.</typeparam>
    /// <param name="oldItem">
    /// The item to replace with <paramref name="newItem"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="newItem">
    /// The item to replace all occurrences of <paramref name="oldItem"/> with. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="comparer">An object that determines whether two instances of <typeparamref name="T"/> are equal.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> with all instances of <paramref name="oldItem"/> replaced 
    /// with <paramref name="newItem"/>; or the current collection if no replacements were made.
    /// </returns>
    public ImmutableObjectCollection Replace<T>(T oldItem, T newItem, IEqualityComparer<T> comparer) where T : class
    {
        ThrowHelpers.ThrowIfNull(newItem);
        ThrowHelpers.ThrowIfNull(oldItem);
        ThrowHelpers.ThrowIfNull(comparer);

        if (!TryGetIndex<T>(out _, out Index entry))
        {
            return this;
        }

        ImmutableArray<object> newItems;
        object[]? itemsArray = RentedArray.Acquire<object>();
        try
        {
            bool modified = false;
            int start = entry.StartIndex, end = start, count = 0;

            for (; end < _items.Length && _items[end] is T existingItem; end++)
            {
                T resultantItem;
                if (comparer.Equals(existingItem, oldItem))
                {
                    modified = true;
                    resultantItem = newItem;
                }
                else
                {
                    resultantItem = existingItem;
                }
                int i = count++;
                RentedArray.Grow(ref itemsArray, count);
                itemsArray[i] = resultantItem;
            }

            if (modified)
            {
                ReadOnlySpan<object> currentItems = _items.AsSpan();
                newItems = [.. currentItems[..start], .. itemsArray.AsSpan(0, count), .. currentItems[end..]];
                return new ImmutableObjectCollection(_itemIndices, newItems);
            }
        }
        finally
        {
            RentedArray.Return(ref itemsArray);
        }

        return this;
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

    private int IndexOf(int id)
    {
        int length = _itemIndices.Length;

        if (length < BinarySearchThreshold)
        {
            for (int i = 0; i < length; i++)
            {
                switch (_itemIndices[i].TypeID)
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
            switch (_itemIndices[mid].TypeID)
            {
                case int value when value == id: return mid;
                case int value when value < id: low = mid + 1; break;
                default: high = mid - 1; break;
            }
        }

        return ~low;
    }

    private bool TryGetIndex<T>(out int index, out Index result) where T : class
    {
        int id = GetID<T>(), length = _itemIndices.Length;
        if (length > 0)
        {
            index = IndexOf(id);
            if (index >= 0)
            {
                result = _itemIndices[index];
                return true;
            }
        }
        else
        {
            index = ~0;
        }
        result = new(id, length);
        return false;
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

    private IEnumerator<object> GetEnumeratorImpl()
    {
        foreach (object item in _items)
        {
            yield return item;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetID<T>() => TypeIdentity<T>.ID;

    private static int NextID() => Interlocked.Increment(ref _currentTypeID);

    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumeratorImpl();
    }
}
