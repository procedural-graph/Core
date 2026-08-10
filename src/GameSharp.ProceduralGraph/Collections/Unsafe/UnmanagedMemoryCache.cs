using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

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
    private static readonly long _defaultCacheSizeKiB;

    /// <inheritdoc/>
    public override long MaxSizeKiB => _defaultCacheSizeKiB;

    static UnmanagedMemoryCache()
    {
#if NET5_0_OR_GREATER
        const int MinMaxSizeKiB = 256 * 1024; // 256 MB
        System.GCMemoryInfo memoryInfo = System.GC.GetGCMemoryInfo();
        _defaultCacheSizeKiB = System.Math.Max(memoryInfo.TotalAvailableMemoryBytes / 4L, MinMaxSizeKiB);
#else
        const int DefaultMaxSize = 1024 * 1024; // 1 GB
        _defaultCacheSizeKiB = DefaultMaxSize;
#endif
    }

    /// <inheritdoc/>
    protected unsafe override long ComputeSize(TSource value)
    {
        return value.Length * sizeof(TValue);
    }

    /// <inheritdoc cref="ConcurrentCache{TKey, TValue}.GetOrAddAsync(TKey, ConcurrentCache{TKey, TValue}.FactoryDelegate, CancellationToken)"/>
    public new async ValueTask<TBase> GetOrAddAsync(TKey key, FactoryDelegate factory, CancellationToken cancellationToken = default)
    {
        TSource result = await base.GetOrAddAsync(key, factory, cancellationToken);
        return result.ShallowCopy();
    }
}
