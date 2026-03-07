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
    /// Executes a specified operation on each row of the map in parallel. 
    /// </summary>
    public unsafe void ForEachRow<TOperation>(TOperation operation) where TOperation : struct, IMapOperation<TValue>
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);

        long height = Height, width = Width;
        using SafeHandle.Scope scope = Handle.GetScoped();
        IntPtr rawSourcePtr = scope;

        Parallel.For(0L, height, y =>
        {
            TValue* rowOffset = (TValue*)rawSourcePtr + (y * width);
            operation.Apply(rowOffset, y, width);
        });
    }

    /// <summary>
    /// Executes an operation that maps elements row-by-row from a source map to a destination map in parallel. 
    /// </summary>
    public unsafe void ForEachRow<TResult, TOperation>(UnmanagedMap<TResult> destination, TOperation operation)
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

        Parallel.For(0L, height, y =>
        {
            TValue* sourceRowOffset = (TValue*)rawSourcePtr + (y * width);
            TResult* destinationRowOffset = (TResult*)rawDestinationPtr + (y * width);
            operation.Apply(sourceRowOffset, destinationRowOffset, y, width);
        });
    }

    /// <summary>
    /// Executes an operation that combines elements row-by-row from two source maps into a destination map in parallel. 
    /// </summary>
    public unsafe void ForEachRow<TSource, TResult, TOperation>(UnmanagedMap<TSource> source, UnmanagedMap<TResult> destination, TOperation operation)
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
            operation.Apply(source1RowOffset, source2RowOffset, destinationRowOffset, y, width);
        });
    }

    /// <summary>
    /// Executes a specified operation on each column of the map in parallel. 
    /// </summary>
    public unsafe void ForEachColumn<TOperation>(TOperation operation) where TOperation : struct, IMapOperation<TValue>
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);

        long height = Height, width = Width;
        using SafeHandle.Scope scope = Handle.GetScoped();
        IntPtr rawSourcePtr = scope;

        Parallel.For(0L, width, x =>
        {
            TValue* columnOffset = (TValue*)rawSourcePtr + x;
            operation.Apply(columnOffset, x, height);
        });
    }

    /// <summary>
    /// Executes an operation that maps elements column-by-column from a source map to a destination map in parallel. 
    /// </summary>
    public unsafe void ForEachColumn<TResult, TOperation>(UnmanagedMap<TResult> destination, TOperation operation)
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

        Parallel.For(0L, width, x =>
        {
            TValue* sourceColumnOffset = (TValue*)rawSourcePtr + x;
            TResult* destinationColumnOffset = (TResult*)rawDestinationPtr + x;
            operation.Apply(sourceColumnOffset, destinationColumnOffset, x, height);
        });
    }

    /// <summary>
    /// Executes an operation that combines elements column-by-column from two source maps into a destination map in parallel. 
    /// </summary>
    public unsafe void ForEachColumn<TSource, TResult, TOperation>(UnmanagedMap<TSource> source, UnmanagedMap<TResult> destination, TOperation operation)
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

        Parallel.For(0L, width, x =>
        {
            TValue* source1ColumnOffset = (TValue*)rawSource1Ptr + x;
            TSource* source2ColumnOffset = (TSource*)rawSource2Ptr + x;
            TResult* destinationColumnOffset = (TResult*)rawDestinationPtr + x;
            operation.Apply(source1ColumnOffset, source2ColumnOffset, destinationColumnOffset, x, height);
        });
    }
}