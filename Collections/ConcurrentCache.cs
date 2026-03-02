using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides a thread-safe, generic cache that supports automatic eviction of entries based on time-to-live and maximum
/// size constraints.
/// </summary>
/// <typeparam name="TKey">The type of the keys used to identify cache entries. Must be a non-nullable type.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the cache. Must be a reference type.</typeparam>
public abstract class ConcurrentCache<TKey, TValue> : IDisposable where TKey : notnull where TValue : class
{
    private readonly record struct CacheEntry(TKey Key, DateTime LastAccessTime);
    private readonly record struct CacheRequest(TKey Key, DateTime RequestTime);

    private static readonly UnboundedChannelOptions _cacheChannelOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _cache = [];
    private readonly Dictionary<TKey, LinkedListNode<CacheEntry>> _lruMap = [];
    private readonly LinkedList<CacheEntry> _lruList = [];
    private readonly Channel<CacheRequest> _lruChannel = Channel.CreateUnbounded<CacheRequest>(_cacheChannelOptions);
    private CancellationTokenSource? _cts = null;

    private bool _disposed;

    /// <summary>
    /// Gets the duration for which the item remains valid before it expires.
    /// </summary>
    public abstract TimeSpan TimeToLive { get; }

    /// <summary>
    /// Gets the maximum allowable size of the cache in bytes.
    /// </summary>
    public abstract long MaxSize { get; }

    private long _currentSize;
    /// <summary>
    /// Gets the current size of the cache in bytes.
    /// </summary>
    public long CurrentSize => _currentSize;

    /// <summary>
    /// Asynchronously retrieves the value associated with the specified key, or adds a new value if the key does not
    /// exist.
    /// </summary>
    /// <param name="key">The key whose value to retrieve or add. This key must be unique within the cache.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the value associated with the
    /// specified key.</returns>
    public async ValueTask<TValue> GetOrAddAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowHelpers.ThrowIf(_disposed, this, ThrowHelpers.CreateObjectDisposedException);
        Task<TValue> task = _cache.GetOrAdd(key, OnAdd).Value;
#if NET8_0_OR_GREATER
        task = task.WaitAsync(cancellationToken);
#endif
        TValue result = await task.ConfigureAwait(false);
        await _lruChannel.Writer.WriteAsync(new CacheRequest(key, DateTime.UtcNow), cancellationToken);
        return result;
    }

    private Lazy<Task<TValue>> OnAdd(TKey key)
    {
        return new Lazy<Task<TValue>>(() => CreateInstanceAsync(key, _cts!.Token), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Asynchronously creates an instance of the <typeparamref name="TValue"/> associated with the provided key.
    /// </summary>
    /// <param name="key">The key that identifies the instance to create. This parameter must not be <see langword="null"/> and should 
    /// correspond to a valid entry in the underlying data source.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation. The task result contains the created 
    /// <typeparamref name="TValue"/>.
    /// </returns>
    protected abstract Task<TValue> CreateInstanceAsync(TKey key, CancellationToken cancellationToken);

    /// <summary>
    /// Processes incoming cache requests asynchronously and manages cache entries until cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to request cancellation of the 
    /// operation.
    /// </param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation of handling cache requests.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this method is called more than once on the same instance.</exception>
    public async Task HandleRequestsAsync(CancellationToken cancellationToken)
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConcurrentCache<,>));
        }
#endif
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Interlocked.CompareExchange(ref _cts, cts, null) is { })
        {
            throw new InvalidOperationException($"{nameof(HandleRequestsAsync)} has already been invoked.");
        }
        try
        {
            await foreach (CacheRequest request in _lruChannel.Reader.ReadAllAsync(cts.Token))
            {
                UpdateCacheEntry(request);
                await EvictCacheEntriesAsync();
            }
        }
        finally
        {
            foreach (Lazy<Task<TValue>> item in _cache.Values)
            {
                if (!item.IsValueCreated || item.Value.Status != TaskStatus.RanToCompletion)
                {
                    continue;
                }

                TValue value = item.Value.GetAwaiter().GetResult();
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    private void UpdateCacheEntry(CacheRequest request)
    {
#if NET6_0_OR_GREATER
        ref LinkedListNode<CacheEntry>? node = ref CollectionsMarshal.GetValueRefOrAddDefault(_lruMap, request.Key, out bool exists);
        if (exists)
        {
            node!.ValueRef = node.Value with { LastAccessTime = request.RequestTime };
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return;
        }
        node = _lruList.AddFirst(new CacheEntry(request.Key, request.RequestTime));
#else
        if (_lruMap.TryGetValue(request.Key, out LinkedListNode<CacheEntry>? node))
        {
            node.Value = node.Value with { LastAccessTime = request.RequestTime };
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return;
        }
        node = _lruList.AddFirst(new CacheEntry(request.Key, request.RequestTime));
        _lruMap.Add(request.Key, node);
#endif
    }

    private async ValueTask EvictCacheEntriesAsync()
    {
        DateTime currentTime = DateTime.UtcNow;
        TimeSpan timeToLive = TimeToLive;

        long maxCacheSizeBytes = MaxSize;

        LinkedListNode<CacheEntry>? nextNode = _lruList.Last;
        LinkedListNode<CacheEntry> currentNode;
        while (nextNode?.Value is { } entry && (_currentSize > maxCacheSizeBytes || (currentTime - entry.LastAccessTime) > timeToLive))
        {
            Lazy<Task<TValue>> item = _cache[entry.Key];
            TValue value = await item.Value;

            Interlocked.Add(ref _currentSize, -ComputeSize(value));

            (currentNode, nextNode) = (nextNode, nextNode.Previous);

            _lruList.Remove(currentNode);
            _lruMap.Remove(entry.Key);
            KeyValuePair<TKey, Lazy<Task<TValue>>> removedItem = new(entry.Key, item);
            ((ICollection<KeyValuePair<TKey, Lazy<Task<TValue>>>>)_cache).Remove(removedItem);

            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// Calculates the size, in bytes, of the specified value.
    /// </summary>
    /// <param name="value">The value for which to compute the size. Cannot be <see langword="null"/>.</param>
    /// <returns>The size of the specified value, in bytes.</returns>
    protected abstract long ComputeSize(TValue value);

    /// <summary>
    /// Releases the resources used by the current instance of the class, optionally disposing of managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; 
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _lruChannel.Writer.Complete();
            if (_cts is { } cts)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}