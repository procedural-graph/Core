using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Provides static methods for creating spans and managed wrappers over blocks of unmanaged memory.
    /// </summary>
    public static unsafe partial class UnmanagedMarshal
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
        public static UnmanagedMap<T> AsUnmanaged<T>(T* pointer, int width, int height) where T : unmanaged
        {
            return new UnmanagedMap<T>(pointer, width, height);
        }

        /// <summary>
        /// Returns a pointer to the first element of the specified unmanaged memory block.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the unmanaged memory. Must be an unmanaged type.</typeparam>
        /// <param name="memory">The unmanaged memory block from which to obtain the pointer.</param>
        /// <returns>A pointer to the first element of the unmanaged memory block represented by <paramref name="memory"/>.</returns>
        public static T* AsPointer<T>(UnmanagedMemory<T> memory) where T : unmanaged
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(memory.disposed, memory);
#else
            if (memory.disposed)
            {
                throw new ObjectDisposedException(memory.GetType().FullName);
            }
#endif
            return memory.buffer;
        }

        internal static long IndexOf<T>(T* buffer, long length, T item, IEqualityComparer<T> equalityComparer) where T : unmanaged
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
}
