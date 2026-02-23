using System;
using System.Collections;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides a base class for managing a contiguous region of unmanaged memory containing elements of type
/// <typeparamref name="T"/>. Supports collection-like operations and resource management for the underlying memory
/// buffer.
/// </summary>
/// <typeparam name="T">The unmanaged value type of elements stored in the memory region.</typeparam>
public abstract unsafe class UnmanagedMemory<T> : ICollection<T>, IEquatable<UnmanagedMemory<T>>, IDisposable where T : unmanaged
{
    /// <summary>
    /// Enumerates the elements of a contiguous memory region.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private T* _current;
        private readonly T* _inclusiveEnd;
        private readonly UnmanagedMemory<T> _parent;

        /// <inheritdoc/>
        public T Current => *_current;
        readonly object IEnumerator.Current => *_current;

        internal Enumerator(UnmanagedMemory<T> parent)
        {
            ThrowHelpers.ThrowIf(parent.disposed, parent, ThrowHelpers.CreateObjectDisposedException);

            _parent = parent;

            if (parent.Length == 0)
            {
                _current = null;
                _inclusiveEnd = null;
                return;
            }

            _current = parent.buffer - 1;
            _inclusiveEnd = _current + parent.Length - 1;
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_current < _inclusiveEnd)
            {
                _current++;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            ThrowHelpers.ThrowIf(_parent.disposed, _parent, ThrowHelpers.CreateObjectDisposedException);
            _current = _parent.buffer - 1;
        }

        readonly void IDisposable.Dispose() { }
    }

    internal volatile bool disposed;

    internal T* buffer;

    /// <inheritdoc cref="ICollection{T}.Count"/>
    public abstract long Length { get; }
    int ICollection<T>.Count => checked((int)Length);

    bool ICollection<T>.IsReadOnly => false;

    /// <inheritdoc/>
    public bool Equals(UnmanagedMemory<T>? other)
    {
        return ReferenceEquals(this, other) || (other is { } && buffer == other.buffer);
    }

    /// <inheritdoc/>
    override public bool Equals(object? obj)
    {
        return obj is UnmanagedMemory<T> other && Equals(other);
    }

    /// <inheritdoc/>
    override public int GetHashCode()
    {
        return ((IntPtr)buffer).GetHashCode();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        UnmanagedMarshal.Clear(buffer, Length);
    }

    /// <inheritdoc/>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc/>
    public abstract bool Contains(T item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(array is null, nameof(array), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf((uint)arrayIndex > array.Length, arrayIndex, ThrowHelpers.CreateArgumentOutOfRangeException);
        if ((array.Length - arrayIndex) < Length)
        {
            throw new ArgumentException($"The number of elements in the source collection is greater than the available space from {nameof(arrayIndex)} to the end of the destination array.");
        }
        fixed (T* destination = &array[arrayIndex])
        {
            UnmanagedMarshal.Copy(buffer, destination, Length);
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; 
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (buffer != null)
        {
            UnmanagedMarshal.Free(buffer, Length);
            buffer = null;
        }

        disposed = true;
    }

    /// <summary>
    /// Releases unmanaged resources held by the <see cref="UnmanagedMemory{T}"/> instance when it is finalized.
    /// </summary>
    ~UnmanagedMemory()
    {
        Dispose(disposing: false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException("Cannot add items to a fixed-size collection.");
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException("Cannot remove items from a fixed-size collection.");
    }
}
