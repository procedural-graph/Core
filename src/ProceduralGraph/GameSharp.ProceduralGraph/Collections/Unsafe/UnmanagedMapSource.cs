using ProceduralGraph;
using System;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

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

    /// <inheritdoc/>
    protected override SafeHandle Handle { get; }

    internal UnmanagedMapSource(SafeHandle handle, long width, long height)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));

        ThrowHelpers.ThrowIfNegative(width);
        Width = width;

        ThrowHelpers.ThrowIfNegative(height);
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
        ThrowHelpers.ThrowIfNegative(width);
        Width = width;

        ThrowHelpers.ThrowIfNegative(height);
        Height = height;

        Length = checked(width * height);

        TValue* buffer = UnmanagedMarshal.AllocZeroed<TValue>(Length, out _);
        Handle = new SafeHandle((IntPtr)buffer);
    }

    /// <inheritdoc/>
    public UnmanagedMap<TValue> DeepCopy()
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
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
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return new UnmanagedMapReference<TValue>(this);
    }

#if NETFRAMEWORK
    object ICloneable.Clone() => DeepCopy();
#endif
}
