using ProceduralGraph;
using System;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a reference to an unmanaged array, providing access to its underlying data source.
/// </summary>
/// <inheritdoc/>
public sealed class UnmanagedArrayReference<TValue> : UnmanagedArray<TValue> where TValue : unmanaged
{
    /// <summary>
    /// Gets the underlying unmanaged array source for the current instance.
    /// </summary>
    public UnmanagedArraySource<TValue> Source { get; }

    /// <inheritdoc/>
    public override long Length => Source.Length;

    /// <inheritdoc/>
    protected override SafeHandle Handle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedArrayReference{TValue}"/> class using the specified unmanaged array source.
    /// </summary>
    /// <param name="source">The unmanaged array source that provides the data for this reference. It cannot be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public UnmanagedArrayReference(UnmanagedArraySource<TValue> source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Handle = source.GetHandle();
        bool success = false;
        Handle.DangerousAddRef(ref success);
        ThrowHelpers.ThrowIfDisposed(!success, this);
        Source = source;
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();
        Handle.DangerousRelease();
    }
}
