using System;

namespace ProceduralGraph;

/// <summary>
/// Provides a base class for objects that require deterministic release of resources, implementing the standard dispose
/// pattern.
/// </summary>
public abstract class ManualDisposable : IDisposable
{
    /// <summary>
    /// Finalizes an instance of the <see cref="ManualDisposable"/> class.
    /// </summary>
    ~ManualDisposable()
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
    /// Attempts to mark the object as disposed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the object is being disposed for the first time; otherwise, <see langword="false"/>.
    /// </returns>
    protected abstract bool TryDispose();

    private void Dispose(bool disposing)
    {
        if (TryDispose())
        {
            if (disposing)
            {
                OnDisposing();
            }
        }

        OnDisposed();
    }
}
