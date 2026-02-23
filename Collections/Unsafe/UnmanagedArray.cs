using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a fixed-size, indexable collection of unmanaged elements allocated in unmanaged memory.
/// </summary>
/// <inheritdoc/>
public sealed class UnmanagedArray<T> : UnmanagedMemory<T>, IList<T>, ICloneable, IStructuralEquatable, IStructuralComparable where T : unmanaged
{
    /// <inheritdoc/>
    public override long Length { get; }

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to retrieve. Must be within the valid range of the collection.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    public unsafe ref T this[long index]
    {
        get
        {
            ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
            ThrowHelpers.ThrowIf((ulong)index < (ulong)Length, index, ThrowHelpers.CreateArgumentOutOfRangeException);
            return ref *(buffer + index);
        }
    }

    T IList<T>.this[int index]
    {
        get => this[index];
        set => this[index] = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMemory{T}"/> class that allocates a zero-initialized buffer for the
    /// specified number of elements.
    /// </summary>
    /// <param name="elementCount">The number of elements to allocate in unmanaged memory. Must be zero or greater.</param>
    public unsafe UnmanagedArray(long elementCount)
    {
        ThrowHelpers.ThrowIf(elementCount < 0L, elementCount, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = elementCount;
        buffer = UnmanagedMarshal.AllocZeroed<T>(elementCount);
    }

    internal unsafe UnmanagedArray(T* buffer, long elementCount)
    {
        ThrowHelpers.ThrowIf(elementCount < 0L, elementCount, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = elementCount;
        this.buffer = buffer;
    }

    /// <inheritdoc cref="IList{T}.IndexOf"/>
    public unsafe long IndexOf(T item)
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
        return UnmanagedMarshal.IndexOf(buffer, Length, item, equalityComparer);
    }

    /// <inheritdoc/>
    public override bool Contains(T item)
    {
        return IndexOf(item) != -1L;
    }

    /// <inheritdoc cref="ICloneable.Clone"/>
    public unsafe UnmanagedMemory<T> Clone()
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);

        T* newBuffer = UnmanagedMarshal.Alloc<T>(Length);
        UnmanagedMarshal.Copy(buffer, newBuffer, Length);

        return new UnmanagedArray<T>(newBuffer, Length);
    }

    /// <inheritdoc cref="IStructuralEquatable.Equals(object?, IEqualityComparer)"/>
    public bool Equals(IEnumerable<T> other, IEqualityComparer<T> comparer)
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(comparer is null, nameof(comparer), ThrowHelpers.CreateArgumentNullException);

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
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(comparer is null, nameof(comparer), ThrowHelpers.CreateArgumentNullException);

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
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(comparer is null, nameof(comparer), ThrowHelpers.CreateArgumentNullException);

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
            case UnmanagedMemory<T> unmanagedMemory:
                length = unmanagedMemory.Length;
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

    int IList<T>.IndexOf(T item)
    {
        checked
        {
            return (int)IndexOf(item);
        }
    }

    object ICloneable.Clone() => Clone();

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
