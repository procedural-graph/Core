using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides a type-safe wrapper for an unmanaged resource handle, ensuring that the handle is released reliably when
/// the object is disposed or finalized.
/// </summary>
public sealed class SafeHandle : System.Runtime.InteropServices.SafeHandle, IEquatable<SafeHandle>
{
    /// <summary>
    /// Represents a scope that manages a native resource.
    /// </summary>
    public ref struct Scope : IDisposable
    {
        private SafeHandle? _handle;

        internal Scope(SafeHandle handle)
        {
            _handle = handle;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _handle, null) is { } handle)
            {
                handle.DangerousRelease();
            }
        }

        private static IntPtr GetHandle(Scope value)
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(value._handle is null, value);
#else
            if (value._handle is null)
            {
                throw new ObjectDisposedException(nameof(Scope));
            }
#endif
            return value._handle.DangerousGetHandle();
        }

        /// <summary>
        /// Implicitly converts a <see cref="Scope"/> instance to an <see cref="IntPtr"/> 
        /// that represents a handle to the underlying resource.
        /// </summary>
        /// <param name="value">The Scope instance to convert to an <see cref="IntPtr"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator IntPtr(Scope value) => GetHandle(value);

        /// <summary>
        /// Implicitly converts a <see cref="Scope"/> instance to a pointer of type <see cref="void"/> 
        /// that represents a handle to the underlying resource.
        /// </summary>
        /// <param name="value">The Scope instance to convert to a pointer of type <see cref="void"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe implicit operator void*(Scope value) => (void*)GetHandle(value);
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

    /// <summary>
    /// Creates a scoped context for the current object, ensuring that it remains valid for the duration of the scope.
    /// </summary>
    /// <returns>A new instance of the <see cref="Scope"/> class that represents the scoped context of the current object.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the current object has already been disposed.</exception>
    public Scope GetScoped()
    {
        bool success = false;
        DangerousAddRef(ref success);
        ThrowHelpers.ThrowIfDisposed(!success, this);
        return new Scope(this);
    }

    /// <inheritdoc/>
    public bool Equals(SafeHandle? other) => other is not null && handle == other.handle;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SafeHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => handle.GetHashCode();

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