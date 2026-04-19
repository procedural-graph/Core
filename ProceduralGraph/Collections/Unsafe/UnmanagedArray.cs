using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a fixed-size, indexable collection of unmanaged elements allocated in unmanaged memory.
/// </summary>
/// <inheritdoc/>
public abstract class UnmanagedArray<T> : UnmanagedMemory<T>, IList<T>, IStructuralEquatable, IStructuralComparable where T : unmanaged
{
    /// <summary>
    /// Gets or sets an element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve. Must be within the valid range of the collection.</param>
    /// <returns>The element at the specified index.</returns>
    public unsafe T this[long index]
    {
        get
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            ThrowHelpers.ThrowIfOutOfRange(index, Length, nameof(index));
            using SafeHandle.Scope scope = Handle.GetScoped();
            return *((T*)(void*)scope + index);
        }
        set
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            ThrowHelpers.ThrowIfOutOfRange(index, Length, nameof(index));
            using SafeHandle.Scope scope = Handle.GetScoped();
            *((T*)(void*)scope + index) = value;
        }
    }

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <inheritdoc cref="IList{T}.IndexOf"/>
    public unsafe long IndexOf(T item)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);

        EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;

        using SafeHandle.Scope scope = Handle.GetScoped();
        T* ptr = (T*)(void*)scope;

        for (long i = 0; i < Length; i++)
        {
            if (equalityComparer.Equals(ptr[i], item))
            {
                return i;
            }
        }

        return -1L;
    }

    /// <inheritdoc/>
    public override bool Contains(T item)
    {
        return IndexOf(item) != -1L;
    }

    /// <inheritdoc cref="IStructuralEquatable.Equals(object?, IEqualityComparer)"/>
    public bool Equals(IEnumerable<T> other, IEqualityComparer<T> comparer)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(comparer);

        if (other is null || (TryGetNonEnumeratedCount(other, out long otherCount) && otherCount != Length))
        {
            return false;
        }

        using IEnumerator<T> otherEnumerator = other.GetEnumerator();
        using Enumerator thisEnumerator = GetEnumerator();
        while (thisEnumerator.MoveNext() && otherEnumerator.MoveNext())
        {
            if (!comparer.Equals(thisEnumerator.Current, otherEnumerator.Current))
            {
                return false;
            }
        }

        return !thisEnumerator.MoveNext() && !otherEnumerator.MoveNext();
    }

    /// <inheritdoc cref="IStructuralEquatable.GetHashCode(IEqualityComparer)"/>
    public int GetHashCode(IEqualityComparer<T> comparer)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(comparer);

        var hash = new HashCode();
        using Enumerator enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            hash.Add(enumerator.Current, comparer);
        }
        return hash.ToHashCode();
    }

    /// <inheritdoc cref="IStructuralComparable.CompareTo(object?, IComparer)"/>
    public int CompareTo(IEnumerable<T>? other, IComparer<T> comparer)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(comparer);

        if (other is null)
        {
            return 1;
        }

        if (TryGetNonEnumeratedCount(other, out long otherCount))
        {
            int lengthComparison = Length.CompareTo(otherCount);
            if (lengthComparison != 0)
            {
                return lengthComparison;
            }
        }

        using Enumerator thisEnumerator = GetEnumerator();
        using IEnumerator<T> otherEnumerator = other.GetEnumerator();

        while (true)
        {
            bool ptrActive = thisEnumerator.MoveNext();
            bool enumActive = otherEnumerator.MoveNext();

            int lengthComparison = ptrActive.CompareTo(otherEnumerator.MoveNext());
            if (lengthComparison != 0)
            {
                return lengthComparison;
            }

            if (!ptrActive)
            {
                return 0;
            }

            int valueComparison = comparer.Compare(thisEnumerator.Current, otherEnumerator.Current);
            if (valueComparison != 0)
            {
                return valueComparison;
            }
        }
    }

    private static bool TryGetNonEnumeratedCount(IEnumerable<T> other, out long length)
    {
        switch (other)
        {
            case IBigCollection<T> collection:
                length = collection.Count;
                return true;
            case ICollection<T> collection:
                length = collection.Count;
                return true;
            case IReadOnlyCollection<T> readOnlyCollection:
                length = readOnlyCollection.Count;
                return true;
            default:
                length = default;
                return false;
        }
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException("Cannot insert items into a fixed-size collection.");
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException("Cannot remove items from a fixed-size collection.");
    }

    int IList<T>.IndexOf(T item) => checked((int)IndexOf(item));

    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
    {
        return other is IEnumerable<T> typedOther && comparer is IEqualityComparer<T> typedComparer && Equals(typedOther, typedComparer);
    }

    int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
    {
        if (comparer is IEqualityComparer<T> typedComparer)
        {
            return GetHashCode(typedComparer);
        }

        throw new ArgumentException($"The comparer must be of type {typeof(IEqualityComparer<T>).FullName}.", nameof(comparer));
    }

    int IStructuralComparable.CompareTo(object? other, IComparer comparer)
    {
        if (comparer is IComparer<T> typedComparer)
        {
            return CompareTo(other as IEnumerable<T>, typedComparer);
        }

        throw new ArgumentException($"The comparer must be of type {typeof(IComparer<T>).FullName}.", nameof(comparer));
    }
}
