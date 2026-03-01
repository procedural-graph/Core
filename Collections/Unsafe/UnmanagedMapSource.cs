using System;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a source of unmanaged memory mapped to a two-dimensional array of elements of type T, providing access to
/// its dimensions and memory management operations.
/// </summary>
/// <inheritdoc/>
public sealed class UnmanagedMapSource<TValue> : UnmanagedMap<TValue>, ICloneable<UnmanagedMap<TValue>> where TValue : unmanaged
{
    /// <inheritdoc/>
    public override long Width { get; }

    /// <inheritdoc/>
    public override long Height { get; }

    /// <inheritdoc/>
    public override long Length { get; }

    internal readonly SafeHandle handle;
    /// <inheritdoc/>
    protected override SafeHandle Handle => handle;

    internal UnmanagedMapSource(SafeHandle handle, long width, long height)
    {
        this.handle = handle ?? throw new ArgumentNullException(nameof(handle));

        ThrowHelpers.ThrowIf(width < 0L, width, ThrowHelpers.CreateArgumentOutOfRangeException);
        Width = width;

        ThrowHelpers.ThrowIf(height < 0L, height, ThrowHelpers.CreateArgumentOutOfRangeException);
        Height = height;

        Length = checked(width * height);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMapSource{T}"/> class with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the unmanaged map source. Must be a non-negative value.</param>
    /// <param name="height">The height of the unmanaged map source. Must be a non-negative value.</param>
    public unsafe UnmanagedMapSource(long width, long height)
    {
        ThrowHelpers.ThrowIf(width < 0L, width, ThrowHelpers.CreateArgumentOutOfRangeException);
        Width = width;

        ThrowHelpers.ThrowIf(height < 0L, height, ThrowHelpers.CreateArgumentOutOfRangeException);
        Height = height;

        Length = checked(width * height);

        TValue* buffer = UnmanagedMarshal.AllocZeroed<TValue>(Length, out _);
        handle = new SafeHandle((IntPtr)buffer);
    }

    /// <inheritdoc/>
    public UnmanagedMap<TValue> DeepCopy()
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        SafeHandle clone = Clone(Handle, Length);
        try
        {
            return new UnmanagedMapSource<TValue>(clone, Width, Height);
        }
        catch
        {
            clone.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public UnmanagedMap<TValue> ShallowCopy()
    {
        ThrowHelpers.ThrowIf(Disposed, this, ThrowHelpers.CreateObjectDisposedException);
        return new UnmanagedMapReference<TValue>(this);
    }

#if NETFRAMEWORK
    object ICloneable.Clone() => DeepCopy();
#endif
}
