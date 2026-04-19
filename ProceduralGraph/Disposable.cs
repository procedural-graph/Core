using System;
using System.Threading;

namespace ProceduralGraph;

/// <summary>
/// Provides a base class for objects that require deterministic release of resources, implementing the standard dispose
/// pattern.
/// </summary>
public abstract class Disposable : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the object has been disposed.
    /// </summary>
#if NET8_0_OR_GREATER
    protected bool Disposed => Volatile.Read(ref _disposed);
    private bool _disposed;
#else
    protected bool Disposed => Volatile.Read(ref _disposed) == 1;
    private int _disposed;
#endif

    /// <summary>
    /// Finalizes an instance of the <see cref="ManualDisposable"/> class.
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

    /// <summary>
    /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> 
    /// to release only unmanaged resources.
    /// </param>
    protected void Dispose(bool disposing)
    {
#if NET8_0_OR_GREATER
        if (!Interlocked.Exchange(ref _disposed, true))
#else                  
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
#endif
        {
            if (disposing)
            {
                OnDisposing();
            }
        }

        OnDisposed();
    }
}