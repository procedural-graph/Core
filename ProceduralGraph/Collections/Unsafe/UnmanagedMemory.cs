using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides a base class for managing a contiguous region of unmanaged memory containing elements of type
/// <typeparamref name="T"/>. Supports collection-like operations and resource management for the underlying memory
/// buffer.
/// </summary>
/// <typeparam name="T">The unmanaged value type of elements stored in the memory region.</typeparam>
public abstract unsafe class UnmanagedMemory<T> : Disposable, IBigCollection<T> where T : unmanaged
{
    /// <summary>
    /// Enumerates the elements of a contiguous memory region.
    /// </summary>
    public ref struct Enumerator : IDisposable
    {
        private T* _current;
        private readonly T* _inclusiveEnd;
        private SafeHandle? _parent;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public T Current => *_current;

        internal Enumerator(SafeHandle parent, long length)
        {
            bool success = false;
            parent.DangerousAddRef(ref success);
            ThrowHelpers.ThrowIfDisposed(!success, parent);

            _parent = parent;

            if (length <= 0)
            {
                _current = null;
                _inclusiveEnd = null;
                return;
            }

            _current = ((T*)parent.DangerousGetHandle()) - 1;
            _inclusiveEnd = _current + length - 1;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
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
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _parent, null) is { } parent)
            {
                parent.DangerousRelease();
            }
        }
    }

    private sealed class AllocEnumerator : IEnumerator<T>
    {
        private T* _current;
        private readonly T* _inclusiveEnd;
        private SafeHandle? _parent;

        public T Current => *_current;
        object IEnumerator.Current => Current;

        public AllocEnumerator(SafeHandle parent, long length)
        {
            bool success = false;
            parent.DangerousAddRef(ref success);
            ThrowHelpers.ThrowIfDisposed(!success, parent);
            _parent = parent;
            if (length <= 0)
            {
                _current = null;
                _inclusiveEnd = null;
                return;
            }
            _current = ((T*)parent.DangerousGetHandle()) - 1;
            _inclusiveEnd = _current + length - 1;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _parent, null) is { } parent)
            {
                parent.DangerousRelease();
            }
        }

        public bool MoveNext()
        {
            if (_current < _inclusiveEnd)
            {
                _current++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            ThrowHelpers.ThrowIfDisposed(_parent is null, this);
            _current = ((T*)_parent.DangerousGetHandle()) - 1;
        }
    }

    /// <summary>
    /// Represents the handle to the unmanaged memory buffer used for data storage.
    /// </summary>
    protected abstract SafeHandle Handle { get; }

    /// <inheritdoc cref="IBigCollection{T}.Count"/>
    public abstract long Length { get; }

    long IBigCollection<T>.Count => Length;

    bool ICollection<T>.IsReadOnly => false;

#if NETFRAMEWORK
    int ICollection<T>.Count => checked((int)Length);
#endif

    /// <inheritdoc/>
    override public bool Equals(object? obj)
    {
        return obj is UnmanagedMemory<T> other && other.Handle.Equals(Handle);
    }

    /// <inheritdoc/>
    override public int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        using SafeHandle.Scope scope = Handle.GetScoped();
        UnmanagedMarshal.Clear((T*)(void*)scope, Length);
    }

    /// <inheritdoc/>
    public Enumerator GetEnumerator() => new(Handle, Length);

    private AllocEnumerator GetAllocatingEnumerator()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return new AllocEnumerator(Handle, Length);
    }

    /// <inheritdoc/>
    public abstract bool Contains(T item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        ThrowHelpers.ThrowIfNull(array);
        ThrowHelpers.ThrowIfOutOfRange(arrayIndex, array.Length);
        if ((array.Length - arrayIndex) < Length)
        {
            throw new ArgumentException($"The number of elements in the source collection is greater than the available space from {nameof(arrayIndex)} to the end of the destination array.");
        }
        using SafeHandle.Scope scope = Handle.GetScoped();
        fixed (T* destination = &array[arrayIndex])
        {
            UnmanagedMarshal.Copy((T*)(void*)scope, destination, Length);
        }   
    }

    /// <summary>
    /// Creates a new <see cref="SafeHandle"/> instance by cloning the memory referenced by the specified handle for a given number of
    /// unmanaged elements.
    /// </summary>
    /// <param name="handle">
    /// The <see cref="SafeHandle"/> instance to clone. Must reference valid, allocated unmanaged 
    /// memory.
    /// </param>
    /// <param name="elementCount">
    /// The number of elements of type <typeparamref name="T"/> to allocate and copy into the new 
    /// <see cref="SafeHandle"/>.
    /// </param>
    /// <returns>
    /// A <see cref="SafeHandle"/> that owns a newly allocated memory region containing a copy of 
    /// the data from the original handle.
    /// </returns>
    protected static SafeHandle Clone(SafeHandle handle, long elementCount)
    {
        SafeHandle.Scope scope = handle.GetScoped();
        void* buffer = null;
        long bytesAllocated = 0L;
        try
        {
            buffer = UnmanagedMarshal.Alloc<T>(elementCount, out bytesAllocated);
            UnmanagedMarshal.Copy((void*)scope, buffer, bytesAllocated);
            return new SafeHandle((IntPtr)buffer);
        }
        catch when (buffer != null)
        {
            UnmanagedMarshal.Free(buffer, bytesAllocated);
            throw;
        }
        finally
        {
            scope.Dispose();
        }
    }

    internal SafeHandle GetHandle()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return Handle;
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        Handle.Dispose();
        GC.RemoveMemoryPressure(Length * sizeof(T));
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetAllocatingEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetAllocatingEnumerator();

    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException("Cannot add items to a fixed-size collection.");
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException("Cannot remove items from a fixed-size collection.");
    }
}
