using ProceduralGraph;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameSharp.ProceduralGraph.Collections;

/// <summary>
/// Provides a type-safe wrapper for an unmanaged resource handle, ensuring that the handle is released reliably when
/// the object is disposed or finalized.
/// </summary>
public sealed partial class SafeHandle : System.Runtime.InteropServices.SafeHandle, IEquatable<SafeHandle>
{
    internal ref struct LeasedHandle : IDisposable
    {
        private readonly SafeHandle _owner;
        private IntPtr _handle;
        public readonly IntPtr Handle => _handle;
        internal LeasedHandle(SafeHandle owner, IntPtr handle)
        {
            _owner = owner;
            _handle = handle;
        }
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _handle, IntPtr.Zero, _handle) != IntPtr.Zero)
            {
                _owner.DangerousRelease();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeHandle"/> class using the specified pointer.
    /// </summary>
    /// <param name="ptr">The handle to be encapsulated by the <see cref="SafeHandle"/> instance. This value must be a valid operating system handle.</param>
    public SafeHandle(IntPtr ptr) : base(IntPtr.Zero, ownsHandle: true)
    {
        SetHandle(ptr);
    }

    /// <inheritdoc/>
    public override bool IsInvalid => handle == IntPtr.Zero;

    /// <inheritdoc/>
    public bool Equals(SafeHandle? other) => other is not null && handle == other.handle;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SafeHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => handle.GetHashCode();

    internal LeasedHandle Lease() => new(this, AddRef());

    /// <inheritdoc/>
    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Increments the reference count and returns a handle to the underlying resource.
    /// </summary>
    /// <remarks>
    /// The caller must ensure that the returned handle is released when it is no longer needed.
    /// </remarks>
    /// <returns>A handle to the underlying resource.</returns>
    [Guard]
    [return: Sentinel(Failure = 0)]
    public partial IntPtr AddRef();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IntPtr AddRefImpl()
    {
        bool success = false;
        DangerousAddRef(ref success);
        return success ? handle : IntPtr.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ObjectDisposedException HandleInvalidResponse<T>(T value, string? message)
    {
        return new ObjectDisposedException(typeof(SafeHandle).FullName, message);
    }

    /// <summary>
    /// Determines whether two specified <see cref="SafeHandle"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="SafeHandle"/> instance to compare.</param>
    /// <param name="right">The second <see cref="SafeHandle"/> instance to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the left and right <see cref="SafeHandle"/> 
    /// instances are equal; otherwise, <see langword="false"/>.
    /// </returns><returns>
    /// <see langword="true"/> if the left and right <see cref="SafeHandle"/> 
    /// instances are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(SafeHandle? left, SafeHandle? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="SafeHandle"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="SafeHandle"/> instance to compare.</param>
    /// <param name="right">The second <see cref="SafeHandle"/> instance to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the left and right <see cref="SafeHandle"/> 
    /// instances are not equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(SafeHandle? left, SafeHandle? right) => !Equals(left, right);
}