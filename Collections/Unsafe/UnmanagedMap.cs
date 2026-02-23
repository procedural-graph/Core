using System;
using System.Collections.Generic;

namespace ProceduralGraph.Collections.Unsafe;

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
            ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
            ThrowHelpers.ThrowIf((ulong)x >= (ulong)Width, y, ThrowHelpers.CreateArgumentOutOfRangeException);
            ThrowHelpers.ThrowIf((ulong)y >= (ulong)Height, y, ThrowHelpers.CreateArgumentOutOfRangeException);
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
        ThrowHelpers.ThrowIf(width < 0L, width, ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height < 0L, height, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = width * height;
        buffer = UnmanagedMarshal.AllocZeroed<T>(Length);
    }

    internal unsafe UnmanagedMap(T* buffer, long width, long height)
    {
        ThrowHelpers.ThrowIf(width < 0L, width, ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height < 0L, height, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = width * height;
        this.buffer = buffer;
    }

    /// <inheritdoc/>
    public unsafe override bool Contains(T item)
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
        return UnmanagedMarshal.IndexOf(buffer, Length, item, equalityComparer) != -1L;
    }
}
