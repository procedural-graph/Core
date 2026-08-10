using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameSharp;

/// <summary>
/// Provides a base class for objects that require deterministic release of resources, implementing the standard dispose
/// pattern.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public abstract class Disposable : IDisposable
{
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
    protected bool Disposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _disposed) != 0;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Disposable"/> class.
    /// </summary>
    ~Disposable()
    {
        Dispose(disposing: false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <remarks>
    /// <para>Invoked exactly once per object lifetime, only if the object is being disposed explicitly (i.e., not from a finalizer).</para>
    /// <para>Override this method to implement custom logic for cleaning up managed state (managed objects).</para>
    /// </remarks>
    /// <inheritdoc cref="Dispose()"/>
    protected virtual void OnDisposing() { }

    /// <remarks>
    /// <para>Will always be invoked, regardless of whether the object is being disposed explicitly or from a finalizer.</para>
    /// <para>Override this method to implement custom logic for cleaning up unmanaged resources (unmanaged objects).</para>
    /// </remarks>
    /// <inheritdoc cref="Dispose()"/>
    protected virtual void OnDisposed() { }

    private void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && disposing)
        {
            OnDisposing();
        }

        OnDisposed();
    }
}