using ProceduralGraph;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides static methods for creating spans and managed wrappers over blocks of unmanaged memory.
/// </summary>
public static partial class UnmanagedMarshal
{
    /// <summary>
    /// Creates an <see cref="UnmanagedArraySource{T}"/> wrapper for a block of memory starting at the specified pointer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the unmanaged memory block. Must be an unmanaged type.</typeparam>
    /// <param name="pointer">A pointer to the first element of the unmanaged memory block to wrap.</param>
    /// <param name="length">The number of elements in the unmanaged memory block. Must be non-negative.</param>
    /// <returns>An <see cref="UnmanagedArray{T}"/> instance representing the specified unmanaged memory region.</returns>
    public static unsafe UnmanagedArraySource<T> AsUnmanaged<T>(T* pointer, int length) where T : unmanaged
    {
        SafeHandle handle = new((IntPtr)pointer);
        try
        {
            return new UnmanagedArraySource<T>(handle, length);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates an <see cref="UnmanagedMapSource{T}"/> wrapper for a block of memory starting at the specified pointer.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the unmanaged memory. Must be an unmanaged type.</typeparam>
    /// <param name="pointer">A pointer to the first element of the unmanaged memory block to wrap.</param>
    /// <param name="width">The number of elements in each row of the 2D memory block. Must be greater than zero.</param>
    /// <param name="height">The number of rows in the 2D memory block. Must be greater than zero.</param>
    /// <returns>An instance of <see cref="UnmanagedMap{T}"/> representing the specified unmanaged 2D memory region.</returns>
    public static unsafe UnmanagedMapSource<T> AsUnmanaged<T>(T* pointer, long width, long height) where T : unmanaged
    {
        SafeHandle handle = new((IntPtr)pointer);
        try
        {
            return new UnmanagedMapSource<T>(handle, width, height);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Gets a safe handle to the unmanaged memory represented by the specified <see cref="UnmanagedMemory{T}"/>
    /// instance.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the unmanaged memory. Must be unmanaged.</typeparam>
    /// <param name="memory">
    /// The <see cref="UnmanagedMemory{T}"/> instance that represents the unmanaged memory. This parameter must not be
    /// <see langword="null"/>.
    /// </param>
    /// <returns>A <see cref="SafeHandle"/> that represents the handle to the unmanaged memory.</returns>
    public static SafeHandle GetHandle<T>(UnmanagedMemory<T> memory) where T : unmanaged
    {
        ThrowHelpers.ThrowIfNull(memory);
        return memory.GetHandle();
    }

    /// <summary>
    /// Allocates a block of unmanaged memory for an array of the specified type and
    /// initializes all bytes to zero.
    /// </summary>
    /// <inheritdoc cref="Alloc{T}(long, out long)"/>
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* AllocZeroed<T>(long elementCount, out long byteCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.AllocZeroed, elementCount, out byteCount);
    }
#else
    public static unsafe T* AllocZeroed<T>(long elementCount, out long byteCount) where T : unmanaged
    {
        void* ptr = Alloc(elementCount, sizeof(T), out byteCount);
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
    /// <param name="byteCount">When this method returns, contains the total number of bytes allocated for the unmanaged memory block.</param>
    /// <returns>
    /// A pointer to the allocated memory block containing zero-initialized elements
    /// of type <typeparamref name="T"/>.
    /// </returns>
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Alloc<T>(long elementCount, out long byteCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.Alloc, elementCount, out byteCount);
    }
#else
    public static unsafe T* Alloc<T>(long elementCount, out long byteCount) where T : unmanaged
    {
        void* ptr = Alloc(elementCount, sizeof(T), out byteCount);
        return (T*)ptr;
    }
#endif

#if NET6_0_OR_GREATER
    private static unsafe T* Alloc<T>(delegate*<nuint, void*> funcPtr, long elementCount, out long byteCount) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elementCount, nameof(elementCount));
        byteCount = elementCount * sizeof(T);
        T* ptr = (T*)funcPtr((nuint)byteCount);
        GC.AddMemoryPressure(byteCount);
        return ptr;
    }
#else
    private static unsafe void* Alloc(long elementCount, int sizeInBytes, out long byteCount)
    {
        ThrowHelpers.ThrowIfNegative(elementCount);
        byteCount = elementCount * sizeInBytes;
        void* ptr = (void*)Marshal.AllocHGlobal((IntPtr)byteCount);
        GC.AddMemoryPressure(byteCount);
        return ptr;
    }
#endif

    /// <summary>
    /// Frees a block of memory.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer. Must be an unmanaged type.</typeparam>
    /// <param name="buffer">
    /// A pointer to the memory buffer containing the elements to be freed. 
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="elementCount">The number of elements in the buffer. Must be greater than zero.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Free<T>(T* buffer, long elementCount) where T : unmanaged
    {
        Free(buffer);
        GC.RemoveMemoryPressure(elementCount * sizeof(T));
    }
#else
    public static unsafe void Free<T>(T* buffer, long elementCount) where T : unmanaged
    {
        Free(buffer);
        GC.RemoveMemoryPressure(elementCount * sizeof(T));
    }
#endif

    /// <inheritdoc cref="Free{T}(T*, long)"/>
    /// <param name="buffer"/>
    /// <param name="byteCount">The number of bytes in the buffer. Must be greater than zero.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Free(void* buffer, long byteCount)
    {
        Free(buffer);
        GC.RemoveMemoryPressure(byteCount);
    }
#else
    public static unsafe void Free(void* buffer, long byteCount)
    {
        Free(buffer);
        GC.RemoveMemoryPressure(byteCount);
    }
#endif

    /// <inheritdoc cref="Free{T}(T*, long)"/>
#if NET6_0_OR_GREATER
    public static unsafe void Free(void* buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        NativeMemory.Free(buffer);
    }
#else
    public static unsafe void Free(void* buffer)
    {
        ThrowHelpers.ThrowIfNull(buffer);
        Marshal.FreeHGlobal((IntPtr)buffer);
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
        ArgumentNullException.ThrowIfNull(buffer);
        NativeMemory.Clear(buffer, (nuint)elementCount * (nuint)sizeof(T));
    }
#else
    public static unsafe void Clear<T>(T* buffer, long elementCount) where T : unmanaged
    {
        ThrowHelpers.ThrowIfNull(buffer);
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
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        NativeMemory.Copy(source, destination, (nuint)elementCount * (nuint)sizeof(T));
    }
#else
    public static unsafe void Copy<T>(T* source, T* destination, long elementCount) where T : unmanaged
    {
        ThrowHelpers.ThrowIfNull(source);
        ThrowHelpers.ThrowIfNull(destination);
        long size = sizeof(T) * elementCount;
        Buffer.MemoryCopy(source, destination, size, size);
    }
#endif

    /// <summary>
    /// Copies a specified number of elements from a source memory location to a destination memory location.
    /// </summary>
    /// <param name="source">A pointer to the source memory location from which elements are copied. Cannot be null.</param>
    /// <param name="destination">A pointer to the destination memory location where elements are copied. Cannot be null.</param>
    /// <param name="byteCount">The number of bytes to copy from the source to the destination. Must be a non-negative integer.</param>
#if NET6_0_OR_GREATER
    public static unsafe void Copy(void* source, void* destination, long byteCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        try
        {
            NativeMemory.Copy(source, destination, nuint.CreateChecked(byteCount));
        }
        catch (OverflowException) when (byteCount < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, null);
        }
    }
#else
    public static unsafe void Copy(void* source, void* destination, long byteCount)
    {
        ThrowHelpers.ThrowIfNull(source);
        ThrowHelpers.ThrowIfNull(destination);
        ThrowHelpers.ThrowIfNegative(byteCount);
        Buffer.MemoryCopy(source, destination, byteCount, byteCount);
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
