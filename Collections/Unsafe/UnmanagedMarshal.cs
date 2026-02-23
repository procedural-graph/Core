using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides static methods for creating spans and managed wrappers over blocks of unmanaged memory.
/// </summary>
public static partial class UnmanagedMarshal
{
    /// <summary>
    /// Creates an <see cref="UnmanagedArray{T}"/> wrapper for a block of memory starting at the specified pointer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the unmanaged memory block. Must be an unmanaged type.</typeparam>
    /// <param name="pointer">A pointer to the first element of the unmanaged memory block to wrap.</param>
    /// <param name="length">The number of elements in the unmanaged memory block. Must be non-negative.</param>
    /// <returns>An <see cref="UnmanagedArray{T}"/> instance representing the specified unmanaged memory region.</returns>
    public static unsafe UnmanagedArray<T> AsUnmanaged<T>(T* pointer, int length) where T : unmanaged
    {
        return new UnmanagedArray<T>(pointer, length);
    }

    /// <summary>
    /// Creates an <see cref="UnmanagedMap{T}"/> wrapper for a block of memory starting at the specified pointer.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the unmanaged memory. Must be an unmanaged type.</typeparam>
    /// <param name="pointer">A pointer to the first element of the unmanaged memory block to wrap.</param>
    /// <param name="width">The number of elements in each row of the 2D memory block. Must be greater than zero.</param>
    /// <param name="height">The number of rows in the 2D memory block. Must be greater than zero.</param>
    /// <returns>An instance of <see cref="UnmanagedMap{T}"/> representing the specified unmanaged 2D memory region.</returns>
    public static unsafe UnmanagedMap<T> AsUnmanaged<T>(T* pointer, int width, int height) where T : unmanaged
    {
        return new UnmanagedMap<T>(pointer, width, height);
    }

    /// <summary>
    /// Returns a pointer to the first element of the specified unmanaged memory block.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the unmanaged memory. Must be an unmanaged type.</typeparam>
    /// <param name="memory">The unmanaged memory block from which to obtain the pointer.</param>
    /// <returns>A pointer to the first element of the unmanaged memory block represented by <paramref name="memory"/>.</returns>
    public static unsafe T* AsPointer<T>(UnmanagedMemory<T> memory) where T : unmanaged
    {
        ThrowHelpers.ThrowIf(memory.disposed, memory, ThrowHelpers.CreateObjectDisposedException);
        return memory.buffer;
    }

    /// <summary>
    /// Allocates a block of unmanaged memory for an array of the specified type and
    /// initializes all bytes to zero.
    /// </summary>
    /// <typeparam name="T">The type of elements to allocate. Must be an unmanaged type.</typeparam>
    /// <param name="elementCount">The number of elements to allocate. Must be greater than zero.</param>
    /// <returns>
    /// A pointer to the allocated memory block containing zero-initialized elements
    /// of type <typeparamref name="T"/>.
    /// </returns>
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* AllocZeroed<T>(long elementCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.AllocZeroed, elementCount);
    }
#else
    public static unsafe T* AllocZeroed<T>(long elementCount) where T : unmanaged
    {
        void* ptr = Alloc(elementCount, sizeof(T), out long byteCount);
        Buffer.MemoryCopy(null, ptr, byteCount, 0);
        return (T*)ptr;
    }
#endif

    /// <summary>
    /// Allocates a block of unmanaged memory sufficient to hold the specified number of
    /// <typeparamref name="T"/> elements.
    /// </summary>
    /// <typeparam name="T">The type of elements to allocate. Must be an unmanaged type.</typeparam>
    /// <param name="elementCount">The number of elements to allocate. Must be greater than zero.</param>
    /// <returns>A pointer to the allocated block of unmanaged memory containing the specified number of elements.</returns>
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Alloc<T>(long elementCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.Alloc, elementCount);
    }
#else
    public static unsafe T* Alloc<T>(long elementCount) where T : unmanaged
    {
        void* ptr = Alloc(elementCount, sizeof(T), out _);
        return (T*)ptr;
    }
#endif

#if NET6_0_OR_GREATER
    private static unsafe T* Alloc<T>(delegate*<nuint, void*> funcPtr, long elementCount) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount, nameof(elementCount));
        long byteCount = elementCount * sizeof(T);
        T* ptr = (T*)funcPtr((nuint)byteCount);
        GC.AddMemoryPressure(byteCount);
        return ptr;
    }
#else
    private static unsafe void* Alloc(long elementCount, int sizeInBytes, out long byteCount)
    {
        ThrowHelpers.ThrowIf(elementCount < 0, elementCount, ThrowHelpers.CreateArgumentOutOfRangeException);
        byteCount = elementCount * sizeInBytes;
        void* ptr = (void*)Marshal.AllocHGlobal((IntPtr)byteCount);
        GC.AddMemoryPressure(byteCount);
        return ptr;
    }
#endif

    /// <summary>
    /// Releases the unmanaged memory allocated for a specified number of <typeparamref name="T"/> elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer. Must be an unmanaged type.</typeparam>
    /// <param name="buffer">A pointer to the memory buffer containing the elements to be freed. Must not be null.</param>
    /// <param name="elementCount">The number of elements in the buffer. Must be greater than zero.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Free<T>(T* buffer, long elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
        NativeMemory.Free(buffer);
        GC.RemoveMemoryPressure(elementCount * sizeof(T));
    }
#else
    public static unsafe void Free<T>(T* buffer, long elementCount) where T : unmanaged
    {
        ThrowHelpers.ThrowIf(buffer == null, nameof(buffer), ThrowHelpers.CreateArgumentNullException);
        Marshal.FreeHGlobal((IntPtr)buffer);
        GC.RemoveMemoryPressure(elementCount * sizeof(T));
    }
#endif

    /// <summary>
    /// Clears the specified buffer by setting all elements to zero.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the buffer. Must be an unmanaged type.</typeparam>
    /// <param name="buffer">A pointer to the buffer whose contents will be cleared.</param>
    /// <param name="elementCount">The number of elements in the buffer to clear. Must be greater than zero.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Clear<T>(T* buffer, long elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
        NativeMemory.Clear(buffer, (nuint)elementCount * (nuint)sizeof(T));
    }
#else
    public static unsafe void Clear<T>(T* buffer, long elementCount) where T : unmanaged
    {
        ThrowHelpers.ThrowIf(buffer == null, nameof(buffer), ThrowHelpers.CreateArgumentNullException);
        long size = sizeof(T) * elementCount;
        Buffer.MemoryCopy(null, buffer, size, 0);
    }
#endif

    /// <summary>
    /// Copies a specified number of elements from a source memory location to a destination memory location.
    /// </summary>
    /// <typeparam name="T">The type of elements to copy. Must be an unmanaged type.</typeparam>
    /// <param name="source">A pointer to the source memory location from which elements are copied. Cannot be null.</param>
    /// <param name="destination">A pointer to the destination memory location where elements are copied. Cannot be null.</param>
    /// <param name="elementCount">The number of elements to copy from the source to the destination. Must be a non-negative integer.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Copy<T>(T* source, T* destination, long elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));
        NativeMemory.Copy(source, destination, (nuint)elementCount * (nuint)sizeof(T));
    }
#else
    public static unsafe void Copy<T>(T* source, T* destination, long elementCount) where T : unmanaged
    {
        ThrowHelpers.ThrowIf(source == null, nameof(source), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(destination == null, nameof(destination), ThrowHelpers.CreateArgumentNullException);
        long size = sizeof(T) * elementCount;
        Buffer.MemoryCopy(source, destination, size, size);
    }
#endif

    internal static unsafe long IndexOf<T>(T* buffer, long length, T item, IEqualityComparer<T> equalityComparer) where T : unmanaged
    {
        for (long i = 0; i < length; i++)
        {
            if (equalityComparer.Equals(buffer[i], item))
            {
                return i;
            }
        }

        return -1L;
    }
}
