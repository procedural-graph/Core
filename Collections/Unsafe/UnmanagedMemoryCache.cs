using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections.Unsafe;

/// <summary>
/// Provides a thread-safe cache for storing and retrieving objects backed by unmanaged memory.
/// </summary>
/// <inheritdoc/>
/// <typeparam name="TKey"/>
/// <typeparam name="TValue">The type of the values stored in unmanaged memory. Must be an unmanaged type.</typeparam>
/// <typeparam name="TBase">The base type representing a block of unmanaged memory for values of type TValue.</typeparam>
/// <typeparam name="TSource">
/// The type of the source objects to be cached, which must be a class that derives from 
/// <typeparamref name="TBase"/> and implements <see cref="ICloneable{TSelf}"/>.</typeparam>
public abstract class UnmanagedMemoryCache<TKey, TValue, TBase, TSource> : ConcurrentCache<TKey, TSource>
    where TKey : notnull
    where TValue : unmanaged
    where TBase : UnmanagedMemory<TValue>
    where TSource : class, TBase, ICloneable<TBase>
{
    private static readonly long _defaultCacheSize;

    /// <inheritdoc/>
    public override long MaxSize { get; }

    static UnmanagedMemoryCache()
    {
#if NET5_0_OR_GREATER
        const int MinMaxSize = 512 * 1024 * 1024; // 512 MB
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
        _defaultCacheSize = Math.Max(memoryInfo.TotalAvailableMemoryBytes / 4L, MinMaxSize);
#else
        const int DefaultMaxSize = 1024 * 1024 * 1024; // 1 GB
        _defaultCacheSize = DefaultMaxSize;
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMemoryCache{TKey, TValue, TBase, TSource}"/> 
    /// class with default maximum size and time-to-live settings.
    /// </summary>
    /// <remarks>
    /// For .NET 5.0 and later, the maximum cache size is determined based on available system memory
    /// to help optimize resource usage. For earlier versions, a predefined default maximum size is used.
    /// </remarks>
    public UnmanagedMemoryCache()
    {
        MaxSize = _defaultCacheSize;
    }

    /// <inheritdoc/>
    protected unsafe override long ComputeSize(TSource value)
    {
        return value.Length * sizeof(TValue);
    }

    /// <inheritdoc cref="ConcurrentCache{TKey, TValue}.GetOrAddAsync(TKey, CancellationToken)"/>
    public new async ValueTask<TBase> GetOrAddAsync(TKey key, CancellationToken cancellationToken = default)
    {
        TSource result = await base.GetOrAddAsync(key, cancellationToken);
        return result.ShallowCopy();
    }
}
