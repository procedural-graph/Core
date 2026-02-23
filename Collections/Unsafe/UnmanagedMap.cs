using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    /// <summary>
    /// Executes a specified operation on each element of the map in parallel. 
    /// </summary>
    /// <typeparam name="TOperation">The type of the operation to apply, which must implement <see cref="IMapOperation{TSource, TSource}"/>.</typeparam>
    /// <param name="operation">The operation to apply to each element.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the map has been disposed.</exception>
    public unsafe void ForEach<TOperation>(TOperation operation) where TOperation : struct, IMapOperation<T, T>
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        long height = Height, width = Width;
        Parallel.For(0L, height, y =>
        {
            T* rowOffset = buffer + (y * width);
            for (long x = 0; x < width; x++)
            {
                ref T valueRef = ref *(rowOffset + x);
                valueRef = operation.Apply(x, y, valueRef);
            }
        });
    }

    /// <summary>
    /// Executes an operation that maps elements from a source map to a destination map in parallel. 
    /// </summary>
    /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
    /// <typeparam name="TOperation">The type of the mapping operation. </typeparam>
    /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
    /// <param name="operation">The operation to apply to each source element to produce a destination element.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when either map has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the dimensions of the source and destination maps do not match.</exception>
    public unsafe void ForEach<TResult, TOperation>(
        UnmanagedMap<TResult> destination,
        TOperation operation)
        where TResult : unmanaged
        where TOperation : struct, IMapOperation<T, TResult>
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(destination is null, nameof(destination), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(destination.disposed, destination, ThrowHelpers.CreateObjectDisposedException);
        long height = Height, width = Width;
        ThrowHelpers.ThrowIf(width != destination.Width, nameof(destination.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height != destination.Height, nameof(destination.Height), ThrowHelpers.CreateArgumentOutOfRangeException);
        TResult* destinationBuffer = destination.buffer;
        Parallel.For(0, height, y =>
        {
            T* sourceRowOffset = buffer + (y * width);
            TResult* destinationRowOffset = destinationBuffer + (y * width);
            for (long x = 0; x < width; x++)
            {
                *(destinationRowOffset + x) = operation.Apply(x, y, in *(sourceRowOffset + x));
            }
        });
    }

    /// <summary>
    /// Executes an operation that combines elements from two source maps into a destination map in parallel. 
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the second source map.</typeparam>
    /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
    /// <typeparam name="TOperation">The type of the dual-source mapping operation. </typeparam>
    /// <param name="source">The second source <see cref="UnmanagedMap{TSource2}"/>.</param>
    /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
    /// <param name="operation">The operation to apply to elements from both sources.</param>
    /// <exception cref="ArgumentNullException">Thrown when any input map is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when map dimensions are mismatched.</exception>
    public unsafe void ForEach<TSource, TResult, TOperation>(
        UnmanagedMap<TSource> source,
        UnmanagedMap<TResult> destination,
        TOperation operation)
        where TSource : unmanaged
        where TResult : unmanaged
        where TOperation : struct, IMapOperation<T, TSource, TResult>
    {
        ThrowHelpers.ThrowIf(disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(source is null, nameof(source), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(source.disposed, source, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(destination is null, nameof(destination), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(destination.disposed, destination, ThrowHelpers.CreateObjectDisposedException);
        long height = Height, width = Width;
        ThrowHelpers.ThrowIf(width != source.Width, nameof(source.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height != source.Height, nameof(source.Height), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(width != destination.Width, nameof(destination.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height != destination.Height, nameof(destination.Height), ThrowHelpers.CreateArgumentOutOfRangeException);
        TSource* source2Buffer = source.buffer;
        TResult* destinationBuffer = destination.buffer;
        Parallel.For(0L, height, y =>
        {
            T* source1RowOffset = buffer + (y * width);
            TSource* source2RowOffset = source2Buffer + (y * width);
            TResult* destinationRowOffset = destinationBuffer + (y * width);
            for (long x = 0; x < width; x++)
            {
                *(destinationRowOffset + x) = operation.Apply(x, y, in *(source1RowOffset + x), in *(source2RowOffset + x));
            }
        });
    }
}
