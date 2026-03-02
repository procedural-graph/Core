namespace ProceduralGraph.Collections.Unsafe;

/// <inheritdoc/>
/// <typeparam name="TKey"/>
/// <typeparam name="TValue">The type of values stored in the cache. This type must be unmanaged.</typeparam>
public abstract class UnmanagedMapCache<TKey, TValue>() : UnmanagedMemoryCache<TKey, TValue, UnmanagedMap<TValue>, UnmanagedMapSource<TValue>>()
     where TKey : notnull
     where TValue : unmanaged;
