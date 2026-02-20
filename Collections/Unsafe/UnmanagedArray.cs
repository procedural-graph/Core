using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Represents a fixed-size, indexable collection of unmanaged elements allocated in unmanaged memory.
    /// </summary>
    /// <inheritdoc/>
    public sealed class UnmanagedArray<T> : UnmanagedMemory<T>, IList<T>, ICloneable, IStructuralEquatable, IStructuralComparable where T : unmanaged
    {
        /// <inheritdoc/>
        public override int Length { get; }

        /// <summary>
        /// Gets a reference to the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve. Must be within the valid range of the collection.</param>
        /// <returns>A reference to the element at the specified index.</returns>
        public unsafe ref T this[int index]
        {
            get
            {
#if NET7_0_OR_GREATER
                ObjectDisposedException.ThrowIf(disposed, GetType());
#else
                if (disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
#endif

                if (index >= 0 && index < Length)
                {
                    return ref *(buffer + index);
                }

                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
        }

        unsafe T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedMemory{T}"/> class that allocates a zero-initialized buffer for the
        /// specified number of elements.
        /// </summary>
        /// <param name="elementCount">The number of elements to allocate in unmanaged memory. Must be zero or greater.</param>
        public unsafe UnmanagedArray(int elementCount)
        {
#if NET7_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(elementCount, nameof(elementCount));
#else
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be zero or greater.");
            }
#endif
            Length = elementCount;
            buffer = UnmanagedMarshal.AllocZeroed<T>(elementCount);
        }

        internal unsafe UnmanagedArray(T* buffer, int elementCount)
        {
#if NET7_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(elementCount, nameof(elementCount));
#else
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be zero or greater.");
            }
#endif
            Length = elementCount;
            this.buffer = buffer;
        }

        /// <inheritdoc cref="ICloneable.Clone"/>
        public unsafe UnmanagedMemory<T> Clone()
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#endif
            T* newBuffer = UnmanagedMarshal.Alloc<T>(Length);
            UnmanagedMarshal.Copy(buffer, newBuffer, Length);
            return new UnmanagedArray<T>(newBuffer, Length);
        }

        /// <inheritdoc cref="IStructuralEquatable.Equals(object?, IEqualityComparer)"/>
        public bool Equals(IEnumerable<T> other, IEqualityComparer<T> comparer)
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#endif

            if (other is null)
            {
                return false;
            }

            if (other.TryGetNonEnumeratedCount(out int otherCount) && otherCount != Length)
            {
                return false;
            }

#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(comparer);
#else
            if (comparer is null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
#endif

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
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(comparer);
#else
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (comparer is null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
#endif
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
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(comparer);
            #else
            if (comparer is null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }
#endif

            if (other is null)
            {
                return 1;
            }

            if (other.TryGetNonEnumeratedCount(out int otherCount))
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

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException("Cannot insert items into a fixed-size collection.");
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException("Cannot remove items from a fixed-size collection.");
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
}
