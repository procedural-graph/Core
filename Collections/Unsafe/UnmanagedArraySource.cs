using System;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a source of unmanaged memory that stores an array of <typeparamref name="TValue"/> elements and provides methods for
/// deep and shallow copying of the array.
/// </summary>
/// <inheritdoc/>
public sealed class UnmanagedArraySource<TValue> : UnmanagedArray<TValue>, ICloneable<UnmanagedArray<TValue>> where TValue : unmanaged
{
    /// <inheritdoc/>
    public override long Length { get; }

    /// <inheritdoc/>
    protected override SafeHandle Handle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedArraySource{T}"/> class that allocates a zero-initialized buffer for the
    /// specified number of elements.
    /// </summary>
    /// <param name="elementCount">The number of elements to allocate in unmanaged array. Must be zero or greater.</param>
    public unsafe UnmanagedArraySource(long elementCount)
    {
        ThrowHelpers.ThrowIfNegative(elementCount);
        Length = elementCount;
        TValue* buffer = UnmanagedMarshal.AllocZeroed<TValue>(elementCount, out _);
        Handle = new SafeHandle((IntPtr)buffer);
    }

    internal UnmanagedArraySource(SafeHandle handle, long elementCount)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        ThrowHelpers.ThrowIfNegative(elementCount);
        Length = elementCount;
    }

    /// <inheritdoc/>
    public UnmanagedArray<TValue> DeepCopy()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        SafeHandle clone = Clone(Handle, Length);
        try
        {
            return new UnmanagedArraySource<TValue>(clone, Length);
        }
        catch
        {
            clone.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public UnmanagedArray<TValue> ShallowCopy()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return new UnmanagedArrayReference<TValue>(this);
    }

#if NETFRAMEWORK
    object ICloneable.Clone() => DeepCopy();
#endif
}
