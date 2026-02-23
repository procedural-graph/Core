using System;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Provides a two-dimensional, fixed-size, contiguous block of unmanaged memory for elements of type <typeparamref name="T"/>, 
    /// supporting collection-like access and manipulation.
    /// </summary>
    /// <inheritdoc/>
    public sealed class UnmanagedMap<T> : UnmanagedMemory<T> where T : unmanaged
    {
        /// <summary>
        /// Gets the width of the two-dimensional memory block.
        /// </summary>
        public long Width { get; }

        /// <summary>
        /// Gets the height of the two-dimensional memory block.
        /// </summary>
        public long Height { get; }

        /// <inheritdoc/>
        public override long Length { get; }

        /// <summary>
        /// Gets a reference to the element at the specified two-dimensional coordinates within the buffer.
        /// </summary>
        /// <param name="x">
        /// The zero-based horizontal index of the element to access. Must be greater than or equal to 0 
        /// and less than <see cref="Width"/>.
        /// </param>
        /// <param name="y">
        /// The zero-based vertical index of the element to access. Must be greater than or equal to 0
        /// and less than <see cref="Height"/>.
        /// </param>
        /// <returns>A reference to the <typeparamref name="T"/> at the specified coordinates.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="x"/> is less than 0 or greater than or equal to <see cref="Width"/>, 
        /// or when <paramref name="y"/> is less than 0 or greater than or equal to <see cref="Height"/>.
        /// </exception>
        public unsafe ref T this[long x, long y]
        {
            get
            {
#if NET7_0_OR_GREATER
                ObjectDisposedException.ThrowIf(disposed, this);
#else
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(UnmanagedMap<T>));
                }
#endif

                if (x < 0L || x > Width)
                {
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
                }

                if (y < 0L || y > Height)
                {
                    throw new ArgumentOutOfRangeException(nameof(y), y, null);
                }

                return ref *(buffer + (y * Width + x));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedMap{T}"/> class with the specified width and height.
        /// </summary>
        /// <param name="width">The number of columns in the 2D memory block. Must be zero or greater.</param>
        /// <param name="height">The number of rows in the 2D memory block. Must be zero or greater.</param>
        public unsafe UnmanagedMap(long width, long height)
        {
#if NET7_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(width, nameof(width));
            Width = width;

            ArgumentOutOfRangeException.ThrowIfNegative(height, nameof(height));
            Height = height;
#else
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, null);
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, null);
            }
#endif

            buffer = UnmanagedMarshal.AllocZeroed<T>(width * height);
        }

        internal unsafe UnmanagedMap(T* buffer, long width, long height)
        {
#if NET7_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfNegative(width, nameof(width));
            Width = width;

            ArgumentOutOfRangeException.ThrowIfNegative(height, nameof(height));
            Height = height;
#else
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, null);
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, null);
            }
#endif

            this.buffer = buffer;
        }

        /// <inheritdoc/>
        public unsafe override bool Contains(T item)
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMap<T>));
            }
#endif
            EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
            return UnmanagedMarshal.IndexOf(buffer, Length, item, equalityComparer) != -1L;
        }
    }
}
