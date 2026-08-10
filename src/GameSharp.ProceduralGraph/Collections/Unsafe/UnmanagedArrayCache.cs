using System;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

/// <inheritdoc cref="UnmanagedMapCache{TKey, TValue}"/>
public sealed class UnmanagedArrayCache<TKey, TValue> : UnmanagedMemoryCache<TKey, TValue, UnmanagedArray<TValue>, UnmanagedArraySource<TValue>>
     where TKey : notnull
     where TValue : unmanaged
{
    /// <inheritdoc/>
    public override TimeSpan TimeToLive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedArrayCache{TKey, TValue}"/> class with a specified 
    /// time-to-live duration for cached items.
    /// </summary>
    /// <param name="timeToLive">
    /// The time interval that determines how long items remain valid in the cache before they expire.
    /// </param>
    public UnmanagedArrayCache(TimeSpan timeToLive)
    {
        TimeToLive = timeToLive;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnmanagedArrayCache{TKey, TValue}"/> class 
    /// with a default time-to-live duration of five minutes for cached items.
    /// </summary>
    public UnmanagedArrayCache()
    {
        TimeToLive = TimeSpan.FromMinutes(5);
    }
}