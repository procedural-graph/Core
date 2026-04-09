using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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

    private static int _currentTypeID;

    private const int BinarySearchThreshold = 16;

    private readonly ImmutableArray<KeyValuePair<int, int>> _itemIndices;

    private readonly ImmutableArray<object> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableObjectCollection"/> class with no items.
    /// </summary>
    public ImmutableObjectCollection()
    {
        _items = [];
        _itemIndices = [];
    }

    private ImmutableObjectCollection(ImmutableArray<KeyValuePair<int, int>> itemIndices, ImmutableArray<object> items)
    {
        _itemIndices = itemIndices;
        _items = items;
    }

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <summary>
    /// Attempts to retrieve a single instance of type T from the collection.
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
        if (_itemIndices.IsDefaultOrEmpty)
        {
            result = null;
            return false;
        }

        int index = IndexOf<T>();

        if (index >= 0)
        {
            int itemStartIndex = _itemIndices[index].Value;
            result = _Unsafe.As<T>(_items[itemStartIndex]);
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
    /// <typeparam name="T">The type of items to retrieve from the collection.</typeparam>
    /// <returns>
    /// A <see cref="ReadOnlySpan{T}"/> containing all contiguous items of type <typeparamref name="T"/> 
    /// found in the collection; or <see cref="ReadOnlySpan{T}.Empty"/> if no such items are present.
    /// </returns>
    public ReadOnlySpan<T> GetMany<T>() where T : class
    {
        if (_itemIndices.IsDefaultOrEmpty)
        {
            return [];
        }

        int idIndex = IndexOf<T>();

        if (idIndex < 0) 
        {
            return [];
        }

        int start = _itemIndices[idIndex].Value, end = start;
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

    /// <summary>
    /// Returns a new collection with the specified item of type <typeparamref name="T"/> added.
    /// </summary>
    /// <typeparam name="T">The type of the item to add.</typeparam>
    /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    public ImmutableObjectCollection Add<T>(T item) where T : class
    {
        ThrowHelpers.ThrowIfNull(item);

        int componentID = GetID<T>();

        int index = IndexOf(componentID);
        KeyValuePair<int, int> entry;

        KeyValuePair<int, int>[] itemIDArray;
        if (index < 0)
        {
            int indicesLength = _itemIndices.Length;
            index = ~index;
            entry = new KeyValuePair<int, int>(componentID, _items.Length);
            itemIDArray = CopyWithPreceding(_itemIndices, index, indicesLength + 1);
            CopyTo(_itemIndices, index, itemIDArray, index + 1, indicesLength - index);
        }
        else
        {
            entry = _itemIndices[index];
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = entry.Value; i < _items.Length && _items[i] is T currentItem; i++)
            {
                if (comparer.Equals(currentItem, item))
                {
                    return this;
                }
            }

            int indicesLength = _itemIndices.Length;
            itemIDArray = CopyWithPreceding(_itemIndices, index, indicesLength);
            for (int i = index + 1; i < indicesLength; i++)
            {
                ref KeyValuePair<int, int> kvp = ref itemIDArray[i];
                kvp = new KeyValuePair<int, int>(kvp.Key, kvp.Value + 1);
            }
        }
        itemIDArray[index] = entry;

        ImmutableArray<KeyValuePair<int, int>> itemIDs = ImmutableCollectionsMarshal.AsImmutableArray(itemIDArray);
        ImmutableArray<object> items = _items.Insert(entry.Value, item);

        return new ImmutableObjectCollection(itemIDs, items);
    }

    /// <summary>
    /// Returns a new collection with all instances of the specified item removed from the collection.
    /// </summary>
    /// <typeparam name="T">The type of the item to remove.</typeparam>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>
    /// A new <see cref="ImmutableObjectCollection"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    public ImmutableObjectCollection Remove<T>(T item) where T : class
    {
        int idIndex = IndexOf<T>();
        if (idIndex < 0)
        {
            return this;
        }
        KeyValuePair<int, int> entry = _itemIndices[idIndex];

        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int start = entry.Value, end = start, matchCount = 0;
        while (end < _items.Length && _items[end] is T candidate)
        {
            if (comparer.Equals(candidate, item))
            {
                matchCount++;
            }
            end++;
        }

        if (matchCount == 0)
        {
            return this;
        }

        int remaining = end - start - matchCount;

        int length = _itemIndices.Length;
        KeyValuePair<int, int>[] itemIDArray;
        if (remaining == 0)
        {
            itemIDArray = CopyWithPreceding(_itemIndices, idIndex, length - 1);
            CopyTo(_itemIndices, idIndex + 1, itemIDArray, idIndex, length - idIndex - 1);
        }
        else
        {
            itemIDArray = CopyWithPreceding(_itemIndices, idIndex, length);
            itemIDArray[idIndex++] = entry;
        }
        for (int i = idIndex; i < itemIDArray.Length; i++)
        {
            ref KeyValuePair<int, int> kvp = ref itemIDArray[i];
            kvp = new KeyValuePair<int, int>(kvp.Key, kvp.Value - matchCount);
        }

        object[] newItemsArray = new object[_items.Length - matchCount];
        CopyTo(_items, 0, newItemsArray, 0, start);
        int destination = start;
        for (int i = start; i < end; i++)
        {
            if (!(_items[i] is T existing && comparer.Equals(existing, item)))
            {
                newItemsArray[destination++] = _items[i];
            }
        }
        CopyTo(_items, end, newItemsArray, destination, _items.Length - end);

        ImmutableArray<KeyValuePair<int, int>> itemIDs = ImmutableCollectionsMarshal.AsImmutableArray(itemIDArray);
        ImmutableArray<object> items = ImmutableCollectionsMarshal.AsImmutableArray(newItemsArray);

        return new ImmutableObjectCollection(itemIDs, items);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOf<T>() where T : class
    {
        int id = GetID<T>();
        return IndexOf(id);
    }

    private int IndexOf(int id)
    {
        int length = _itemIndices.Length;

        if (length < BinarySearchThreshold)
        {
            for (int i = 0; i < length; i++)
            {
                switch (_itemIndices[i].Key)
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
            switch (_itemIndices[mid].Key)
            {
                case int value when value == id: return mid;
                case int value when value < id: low = mid + 1; break;
                default: high = mid - 1; break;
            }
        }

        return ~low;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] CopyWithPreceding<T>(ImmutableArray<T> source, int index, int length)
    {
#if NET5_0_OR_GREATER
        T[] itemIDs = GC.AllocateUninitializedArray<T>(length);
#else
        T[] itemIDs = new T[length];
#endif
        CopyTo(source, 0, itemIDs, 0, index);
        return itemIDs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyTo<T>(ImmutableArray<T> source, int sourceIndex, T[] destination, int destinationIndex, int count)
    {
        ReadOnlySpan<T> sourceSpan = source.AsSpan(sourceIndex, count);
        Span<T> destinationSpan = destination.AsSpan(destinationIndex, count);
        sourceSpan.CopyTo(destinationSpan);
    }

    private IEnumerator<object> GetEnumeratorImpl()
    {
        object[] components = ImmutableCollectionsMarshal.AsArray(this._items)!;
        return (IEnumerator<object>)components.GetEnumerator();
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
