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

    internal readonly SafeHandle handle;
    /// <inheritdoc/>
    protected override SafeHandle Handle => handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedArraySource{T}"/> class that allocates a zero-initialized buffer for the
    /// specified number of elements.
    /// </summary>
    /// <param name="elementCount">The number of elements to allocate in unmanaged array. Must be zero or greater.</param>
    public unsafe UnmanagedArraySource(long elementCount)
    {
        ThrowHelpers.ThrowIf(elementCount < 0L, elementCount, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = elementCount;
        TValue* buffer = UnmanagedMarshal.AllocZeroed<TValue>(elementCount, out _);
        handle = new SafeHandle((IntPtr)buffer);
    }

    internal UnmanagedArraySource(SafeHandle handle, long elementCount)
    {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
        ThrowHelpers.ThrowIf(elementCount < 0L, elementCount, ThrowHelpers.CreateArgumentOutOfRangeException);
        Length = elementCount;
    }

    /// <inheritdoc/>
    public UnmanagedArray<TValue> DeepCopy()
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
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
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        return new UnmanagedArrayReference<TValue>(this);
    }

#if NETFRAMEWORK
    object ICloneable.Clone() => DeepCopy();
#endif
}
