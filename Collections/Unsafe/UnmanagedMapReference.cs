namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Represents a reference to an existing <see cref="UnmanagedMap{T}"/> instance, sharing the same underlying
/// unmanaged memory buffer without taking ownership of it.
/// </summary>
/// <inheritdoc/>
public sealed class UnmanagedMapReference<T> : UnmanagedMap<T> where T : unmanaged
{
    /// <summary>
    /// Gets the unmanaged map source associated with this instance.
    /// </summary>
    public UnmanagedMapSource<T> Source { get; }

    /// <inheritdoc/>
    public override long Width => Source.Width;

    /// <inheritdoc/>
    public override long Height => Source.Height;

    /// <inheritdoc/>
    public override long Length => Source.Length;

    /// <inheritdoc/>
    protected override SafeHandle Handle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMapReference{T}"/> class using the specified unmanaged map as the owner.
    /// </summary>
    /// <param name="source">The <see cref="UnmanagedMap{T}"/> instance that owns the underlying buffer. This parameter must not be null.</param>
    public UnmanagedMapReference(UnmanagedMapSource<T> source)
    {
        ThrowHelpers.ThrowIf(source is null, nameof(source), ThrowHelpers.CreateArgumentNullException);
        Handle = source.GetHandle();
        bool success = false;
        Handle.DangerousAddRef(ref success);
        ThrowHelpers.ThrowIf(!success, nameof(source), ThrowHelpers.CreateObjectDisposedException);
        Source = source;
    }

    /// <inheritdoc/>
    protected override void Disposing()
    {
        Handle.DangerousRelease();
    }
}
