using System;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Provides static methods for creating spans and managed wrappers over blocks of unmanaged memory.
    /// </summary>
    public static unsafe partial class UnmanagedMarshal
    {
        /// <summary>
        /// Creates a span over the contents of the specified unmanaged memory.
        /// </summary>
        /// <remarks>Do not use the span after the array is disposed.</remarks>
        /// <typeparam name="T">The type of elements in the unmanaged memory. Must be an unmanaged type.</typeparam>
        /// <param name="memory">The unmanaged memory to create the span from.</param>
        /// <returns>A span that represents the elements of the specified unmanaged memory.</returns>
        public static Span<T> AsSpan<T>(UnmanagedMemory<T> memory) where T : unmanaged
        {
            T* pointer = AsPointer(memory);
            return new Span<T>(pointer, memory.Length);
        }

        /// <summary>
        /// Creates a span over the contents of the specified unmanaged memory.
        /// </summary>
        /// <remarks>Do not use the span after the array is disposed.</remarks>
        /// <typeparam name="T">The type of elements in the unmanaged memory. Must be an unmanaged type.</typeparam>
        /// <param name="memory">The unmanaged memory to create the span from.</param>
        /// <param name="start">The index of the first element in the unmanaged memory to include in the span.</param>
        /// <returns>A span that represents the elements of the specified unmanaged memory.</returns>
        public static Span<T> AsSpan<T>(UnmanagedMemory<T> memory, int start) where T : unmanaged
        {
#if NET7_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(start, memory.Length, nameof(start));
            ArgumentOutOfRangeException.ThrowIfNegative(start, nameof(start));
#else
            if (start < 0 || start > memory.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
#endif
            T* pointer = AsPointer(memory);
            return new Span<T>(pointer + start, memory.Length - start);
        }

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
    }
}
