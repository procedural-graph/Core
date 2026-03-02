namespace ProceduralGraph.Collections.Unsafe;

/// <inheritdoc cref="UnmanagedMapCache{TKey, TValue}"/>
public abstract class UnmanagedArrayCache<TKey, TValue>() : UnmanagedMemoryCache<TKey, TValue, UnmanagedArray<TValue>, UnmanagedArraySource<TValue>>()
     where TKey : notnull
     where TValue : unmanaged;