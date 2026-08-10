using System;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

/// <inheritdoc/>
/// <typeparam name="TKey"/>
/// <typeparam name="TValue">The type of values stored in the cache. This type must be unmanaged.</typeparam>
public sealed class UnmanagedMapCache<TKey, TValue> : UnmanagedMemoryCache<TKey, TValue, UnmanagedMap<TValue>, UnmanagedMapSource<TValue>>
     where TKey : notnull
     where TValue : unmanaged
{
    /// <inheritdoc/>
    public override TimeSpan TimeToLive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMapCache{TKey, TValue}"/> class with a specified 
    /// time-to-live duration for cached items.
    /// </summary>
    /// <param name="timeToLive">
    /// The time interval that determines how long items remain valid in the cache before they expire.
    /// </param>
    public UnmanagedMapCache(TimeSpan timeToLive)
    {
        TimeToLive = timeToLive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedMapCache{TKey, TValue}"/> class 
    /// with a default time-to-live duration of five minutes for cached items.
    /// </summary>
    public UnmanagedMapCache()
    {
        TimeToLive = TimeSpan.FromMinutes(5);
    }
}
