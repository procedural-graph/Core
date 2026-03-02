using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides a two-dimensional, fixed-size, contiguous block of unmanaged memory for elements of type <typeparamref name="TValue"/>, 
/// supporting collection-like access and manipulation.
/// </summary>
/// <typeparam name="TValue">The unmanaged value type of elements stored in the memory region.</typeparam>
public abstract class UnmanagedMap<TValue> : UnmanagedMemory<TValue>, IBigCollection<TValue> where TValue : unmanaged
{
    /// <summary>
    /// Gets the width of the two-dimensional memory block.
    /// </summary>
    public abstract long Width { get; }

    /// <summary>
    /// Gets the height of the two-dimensional memory block.
    /// </summary>
    public abstract long Height { get; }

    /// <summary>
    /// Gets or sets the element at the specified two-dimensional coordinates within the buffer.
    /// </summary>
    /// <param name="x">
    /// The zero-based horizontal index of the element to access. Must be greater than or equal to 0 
    /// and less than <see cref="Width"/>.
    /// </param>
    /// <param name="y">
    /// The zero-based vertical index of the element to access. Must be greater than or equal to 0
    /// and less than <see cref="Height"/>.
    /// </param>
    /// <returns>The <typeparamref name="T"/> at the specified coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="x"/> is less than 0 or greater than or equal to <see cref="Width"/>, 
    /// or when <paramref name="y"/> is less than 0 or greater than or equal to <see cref="Height"/>.
    /// </exception>
    public unsafe TValue this[long x, long y]
    {
        get
        {
            ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
            ThrowHelpers.ThrowIf((ulong)x >= (ulong)Width, y, ThrowHelpers.CreateArgumentOutOfRangeException);
            ThrowHelpers.ThrowIf((ulong)y >= (ulong)Height, y, ThrowHelpers.CreateArgumentOutOfRangeException);
            using SafeHandle.Scope scope = Handle.GetScoped();
            return *((TValue*)scope + (y * Width + x));
        }
        set
        {
            ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
            ThrowHelpers.ThrowIf((ulong)x >= (ulong)Width, y, ThrowHelpers.CreateArgumentOutOfRangeException);
            ThrowHelpers.ThrowIf((ulong)y >= (ulong)Height, y, ThrowHelpers.CreateArgumentOutOfRangeException);
            using SafeHandle.Scope scope = Handle.GetScoped();
            *((TValue*)scope + (y * Width + x)) = value;
        }
    }

    /// <inheritdoc/>
    public unsafe override bool Contains(TValue item)
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        EqualityComparer<TValue> equalityComparer = EqualityComparer<TValue>.Default;
        using SafeHandle.Scope scope = Handle.GetScoped();
        return UnmanagedMarshal.IndexOf((TValue*)scope, Length, item, equalityComparer) != -1L;
    }

    /// <summary>
    /// Executes a specified operation on each element of the map in parallel. 
    /// </summary>
    /// <typeparam name="TOperation">The type of the operation to apply, which must implement <see cref="IMapOperation{TSource, TSource}"/>.</typeparam>
    /// <param name="operation">The operation to apply to each element.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the map has been disposed.</exception>
    public unsafe void ForEach<TOperation>(TOperation operation) where TOperation : struct, IMapOperation<TValue, TValue>
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);

        long height = Height, width = Width;
        using SafeHandle.Scope scope = Handle.GetScoped();
        IntPtr rawSourcePtr = scope;

        Parallel.For(0L, height, y =>
        {
            TValue* rowOffset = (TValue*)rawSourcePtr + (y * width);
            for (long x = 0; x < width; x++)
            {
                ref TValue valueRef = ref *(rowOffset + x);
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
    public unsafe void ForEach<TResult, TOperation>(UnmanagedMap<TResult> destination, TOperation operation)
        where TResult : unmanaged
        where TOperation : struct, IMapOperation<TValue, TResult>
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(destination is null, nameof(destination), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(Width != destination.Width, nameof(destination.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(Height != destination.Height, nameof(destination.Height), ThrowHelpers.CreateArgumentOutOfRangeException);

        long height = Height, width = Width;
        using SafeHandle.Scope sourceScope = Handle.GetScoped();
        IntPtr rawSourcePtr = sourceScope;

        using SafeHandle.Scope destinationScope = destination.Handle.GetScoped();
        IntPtr rawDestinationPtr = destinationScope;

        Parallel.For(0, height, y =>
        {
            TValue* sourceRowOffset = (TValue*)rawSourcePtr + (y * width);
            TResult* destinationRowOffset = (TResult*)rawDestinationPtr + (y * width);
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
    public unsafe void ForEach<TSource, TResult, TOperation>(UnmanagedMap<TSource> source, UnmanagedMap<TResult> destination, TOperation operation)
        where TSource : unmanaged
        where TResult : unmanaged
        where TOperation : struct, IMapOperation<TValue, TSource, TResult>
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        ThrowHelpers.ThrowIf(source is null, nameof(source), ThrowHelpers.CreateArgumentNullException);
        ThrowHelpers.ThrowIf(destination is null, nameof(destination), ThrowHelpers.CreateArgumentNullException);

        long height = Height, width = Width;
        ThrowHelpers.ThrowIf(width != source.Width, nameof(source.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height != source.Height, nameof(source.Height), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(width != destination.Width, nameof(destination.Width), ThrowHelpers.CreateArgumentOutOfRangeException);
        ThrowHelpers.ThrowIf(height != destination.Height, nameof(destination.Height), ThrowHelpers.CreateArgumentOutOfRangeException);

        using SafeHandle.Scope source1Handle = Handle.GetScoped();
        IntPtr rawSource1Ptr = source1Handle;

        using SafeHandle.Scope source2Handle = source.Handle.GetScoped();
        IntPtr rawSource2Ptr = source2Handle;

        using SafeHandle.Scope destinationHandle = destination.Handle.GetScoped();
        IntPtr rawDestinationPtr = destinationHandle;

        Parallel.For(0L, height, y =>
        {
            TValue* source1RowOffset = (TValue*)rawSource1Ptr + (y * width);
            TSource* source2RowOffset = (TSource*)rawSource2Ptr + (y * width);
            TResult* destinationRowOffset = (TResult*)rawDestinationPtr + (y * width);
            for (long x = 0; x < width; x++)
            {
                *(destinationRowOffset + x) = operation.Apply(x, y, in *(source1RowOffset + x), in *(source2RowOffset + x));
            }
        });
    }
}
