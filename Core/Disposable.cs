using System;
using System.Threading;

namespace ProceduralGraph;

/// <inheritdoc/>
public abstract class Disposable : ManualDisposable
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

    /// <inheritdoc/>
    protected override bool TryDispose()
    {
#if NET8_0_OR_GREATER
        return !Interlocked.Exchange(ref _disposed, true);
#else                  
        return Interlocked.Exchange(ref _disposed, 1) == 0;
#endif
    }
}