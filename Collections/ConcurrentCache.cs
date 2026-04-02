using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides a thread-safe, generic cache that supports automatic eviction of entries based on time-to-live and maximum
/// size constraints.
/// </summary>
/// <typeparam name="TKey">The type of the keys used to identify cache entries. Must be a non-nullable type.</typeparam>
/// <typeparam name="TValue">The type of the values stored in the cache. Must be a reference type.</typeparam>
public abstract class ConcurrentCache<TKey, TValue> : IDisposable where TKey : notnull where TValue : class
{
    private ref struct ReverseChronologyEnumerator(LinkedList<AccessLog> chronology)
    {
        private readonly LinkedList<AccessLog> _chronology = chronology;

        private LinkedListNode<AccessLog>? _expectedNextNode;
        private LinkedListNode<AccessLog>? _currentNode;
        
        private bool _started;

        public bool MoveNext([NotNullWhen(true)] out LinkedListNode<AccessLog>? currentNode)
        {
            currentNode = null;

            if (_currentNode is null)
            {
                if (_started)
                {
                    return false;
                }

                lock (_chronology)
                {
                    _currentNode = _chronology.Last;
                    _expectedNextNode = _currentNode?.Previous;
                }

                _started = true;
            }
            else
            {
                lock (_chronology)
                {
                    if (ReferenceEquals(_currentNode?.List, _chronology) && ReferenceEquals(_currentNode.Previous, _expectedNextNode))
                    {
                        _currentNode = _expectedNextNode;
                        _expectedNextNode = _expectedNextNode?.Previous;
                    }
                    else
                    {
                        _currentNode = null;
                        _expectedNextNode = null;
                        return false;
                    }
                }
            }

            currentNode = _currentNode;
            return currentNode is { };
        }
    }

    /// <summary>
    /// Represents a delegate that asynchronously creates a <typeparamref name="TValue"/> based on the specified 
    /// <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key used to generate the value. The value returned by the delegate is determined by this key.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous value creation operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation and contains the generated 
    /// <typeparamref name="TValue"/>.
    /// </returns>
    public delegate ValueTask<TValue> FactoryDelegate(TKey key, CancellationToken cancellationToken);

    private readonly record struct Entry(Task<TValue> Item, LinkedListNode<AccessLog> LogNode, uint SizeKiB);
    private readonly record struct AccessLog(TKey Key, DateTime LastAccessTime);
    private readonly record struct AccessQuery(TKey Key, DateTime RequestTime);

    private static readonly BoundedChannelOptions _channelOptions = new(100)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropOldest
    };

    private readonly ConcurrentDictionary<TKey, Entry> _entries = [];
    private readonly LinkedList<AccessLog> _chronology = [];
    private readonly Channel<AccessQuery> _queries = Channel.CreateBounded<AccessQuery>(_channelOptions);
    private CancellationTokenSource? _cts = null;

    private bool _disposed;

    /// <summary>
    /// Gets the duration for which the item remains valid before it expires.
    /// </summary>
    public abstract TimeSpan TimeToLive { get; }

    /// <summary>
    /// Gets the maximum allowable size of the cache in kibibytes.
    /// </summary>
    public abstract long MaxSizeKiB { get; }

    private long _currentSizeKiB;
    /// <summary>
    /// Gets the current size of the cache in kibibytes.
    /// </summary>
    public long CurrentSizeKiB => _currentSizeKiB;

    /// <summary>
    /// Asynchronously retrieves the value associated with the specified key, or adds a new value if the key does not
    /// exist.
    /// </summary>
    /// <param name="key">The key whose value to retrieve or add. This key must be unique within the cache.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
    /// <param name="factory">
    /// A delegate that is invoked to create a value if the specified key does not exist in the cache. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the value associated with the
    /// specified key.</returns>
    public async ValueTask<TValue> GetOrAddAsync(TKey key, FactoryDelegate factory, CancellationToken cancellationToken = default)
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);
        Entry entry = _entries.GetOrAdd(key, OnAdd, factory);
        _queries.Writer.TryWrite(new AccessQuery(key, DateTime.UtcNow));
        return await entry.Item.WaitAsync(cancellationToken);
    }

    private Entry OnAdd(TKey key, FactoryDelegate factory)
    {
        const TaskContinuationOptions ContinuationOptions = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnRanToCompletion;

        LinkedListNode<AccessLog> node = new(new AccessLog(key, DateTime.UtcNow));
        lock (_chronology)
        {
            _chronology.AddFirst(node);
        }

        Task<TValue> creation = Add(key, factory);
        Tuple<ConcurrentCache<TKey, TValue>, TKey> state = new(this, key);
        _ = creation.ContinueWith(OnCreationFaulted, state, ContinuationOptions);

        return new Entry(creation, node, default);
    }

    /// <summary>
    /// Asynchronously removes the cache entry associated with the specified key and returns its value if the entry was
    /// present.
    /// </summary>
    /// <param name="key">The key of the cache entry to remove. The key must exist in the cache for the operation to succeed.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation. 
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous operation. The task result contains the value associated with the
    /// specified key if it was successfully removed from the cache; otherwise, <see langword="null"/>.
    /// </returns>
    public async ValueTask<TValue?> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);

        if (!TryRemove(key, out Entry result))
        {
            return null;
        }

        try
        {
            TValue value = await result.Item.WaitAsync(cancellationToken);
            Interlocked.Add(ref _currentSizeKiB, -result.SizeKiB);
            return value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            CleanupEntry(ref _currentSizeKiB, in result);
            throw;
        }
    }

    /// <summary>
    /// Invalidates the cache entry associated with the specified key, marking it for removal.
    /// </summary>
    /// <param name="key">The key of the cache entry to invalidate. This key must not be <see langword="null"/>.</param>
    /// <returns>
    /// Returns <see langword="true"/> if the cache entry was successfully invalidated; otherwise, 
    /// <see langword="false"/>.
    /// </returns>
    public bool Invalidate(TKey key)
    {
        ThrowHelpers.ThrowIfDisposed(_disposed, this);

        if (TryRemove(key, out Entry result))
        {
            CleanupEntry(ref _currentSizeKiB, in result);
            return true;
        }

        return false;
    }

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
        ThrowHelpers.ThrowIfDisposed(_disposed, this);
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (Interlocked.CompareExchange(ref _cts, cts, null) is { })
        {
            throw new InvalidOperationException($"{nameof(HandleRequestsAsync)} has already been invoked.");
        }
        try
        {
            await foreach (AccessQuery request in _queries.Reader.ReadAllAsync(cts.Token))
            {
                UpdateCacheEntry(request);
                EvictCacheEntries();
            }
        }
        finally
        {
            foreach (Entry entry in _entries.Values)
            {
                CleanupEntry(in entry);
            }
        }
    }

    private void UpdateCacheEntry(AccessQuery request)
    {
        LinkedListNode<AccessLog>? node;
        Entry newValue, oldValue;

        do
        {
            if (!_entries.TryGetValue(request.Key, out oldValue))
            {
                return;
            }

            node = oldValue.LogNode ?? new LinkedListNode<AccessLog>(default);

            newValue = oldValue with { LogNode = node };
        }
        while (!_entries.TryUpdate(request.Key, newValue, oldValue));

        lock (_chronology)
        {
            node.Value = new AccessLog(request.Key, request.RequestTime);
            node.List?.Remove(node);
            _chronology.AddFirst(node);
        }
    }

    private void EvictCacheEntries()
    {
        long maxSizeKiB = MaxSizeKiB;
        TimeSpan timeToLive = TimeToLive;
        DateTime currentTime = DateTime.UtcNow;
        do
        {
            ReverseChronologyEnumerator enumerator = new(_chronology);
            LinkedListNode<AccessLog>? previousNode = null;
            while (enumerator.MoveNext(out LinkedListNode<AccessLog>? currentNode))
            {
                AccessLog log;
                lock (_chronology)
                {
                    log = currentNode.Value;
                    if (previousNode is { } && ReferenceEquals(previousNode.List, _chronology))
                    {
                        _chronology.Remove(previousNode);
                    }
                }

                previousNode = null;

                if (_currentSizeKiB < maxSizeKiB && (currentTime - log.LastAccessTime) < timeToLive)
                {
                    return;
                }

                while (_entries.TryGetValue(log.Key, out Entry entry) && ReferenceEquals(entry.LogNode, currentNode) && entry.Item.IsCompleted)
                {
                    KeyValuePair<TKey, Entry> kvp = new(log.Key, entry);
                    if (((ICollection<KeyValuePair<TKey, Entry>>)_entries).Remove(kvp))
                    {
                        CleanupEntry(ref _currentSizeKiB, in entry);
                        previousNode = currentNode;
                        break;
                    }
                }
            }

            if (previousNode is null)
            {
                continue;
            }

            lock (_chronology)
            {
                if (ReferenceEquals(previousNode.List, _chronology))
                {
                    _chronology.Remove(previousNode);
                }
            }
        }
        while (_currentSizeKiB > maxSizeKiB);
    }

    private bool TryRemove(TKey key, out Entry entry)
    {
        if (!_entries.TryRemove(key, out entry))
        {
            return false;
        }

        lock (_chronology)
        {
            LinkedListNode<AccessLog> logNode = entry.LogNode;
            if (ReferenceEquals(logNode.List, _chronology))
            {
                _chronology.Remove(entry.LogNode);
            }
        }

        return true;
    }

    /// <summary>
    /// Calculates the size, in bytes, of the specified value.
    /// </summary>
    /// <remarks>
    /// Values larger than 4096 GiB will be treated as 4096 GiB, and values smaller than 0 will be treated as 0.
    /// </remarks>
    /// <param name="value">The value for which to compute the size. Cannot be <see langword="null"/>.</param>
    /// <returns>The size of the specified value, in bytes.</returns>
    protected abstract long ComputeSize(TValue value);

    private uint ComputeSizeKib(TValue value)
    {
        long sizeInBytes = ComputeSize(value);
#if NET7_0_OR_GREATER
        return uint.CreateSaturating(sizeInBytes / 1024);
#else
        long sizeInKib = sizeInBytes / 1024;
        return sizeInKib switch
        {
            < uint.MinValue => uint.MinValue,
            > uint.MaxValue => uint.MaxValue,
            _ => (uint)sizeInKib
        };
#endif
    }

    private async Task<TValue> Add(TKey key, FactoryDelegate factory)
    {
        TValue value = await factory(key, _cts!.Token);
        uint sizeKib = ComputeSizeKib(value);

        Entry currentValue, newValue;
        do
        {
            if (!_entries.TryGetValue(key, out currentValue))
            {
                return value;
            }

            newValue = currentValue with { SizeKiB = sizeKib };
        }
        while (!_entries.TryUpdate(key, newValue, currentValue));

        Interlocked.Add(ref _currentSizeKiB, sizeKib);

        return value;
    }

    private static void CleanupEntry(ref long currentSizeKiB, in Entry entry)
    {
        Interlocked.Add(ref currentSizeKiB, -entry.SizeKiB);
        CleanupEntry(entry);
    }

    private static void CleanupEntry(in Entry entry)
    {
        const TaskContinuationOptions ContinuationOptions = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion;
        Task<TValue> creation = entry.Item;
        switch (creation.Status)
        {
            case TaskStatus.RanToCompletion: DisposeInvalidated(creation); break;
            case TaskStatus.Faulted or TaskStatus.Canceled: break;
            default: creation.ContinueWith(DisposeInvalidated, ContinuationOptions); break;
        }
    }

    private static void OnCreationFaulted(Task<TValue> creation, object? obj)
    {
        (ConcurrentCache<TKey, TValue> cache, TKey key) = (Tuple<ConcurrentCache<TKey, TValue>, TKey>)obj!;
        while (cache._entries.TryGetValue(key, out Entry entry) && ReferenceEquals(entry.Item, creation))
        {
            KeyValuePair<TKey, Entry> kvp = new(key, entry);
            if (!((ICollection<KeyValuePair<TKey, Entry>>)cache._entries).Remove(kvp))
            {
                continue;
            }

            lock (cache._chronology)
            {
                LinkedListNode<AccessLog> node = entry.LogNode;
                if (ReferenceEquals(node.List, cache._chronology))
                {
                    cache._chronology.Remove(node);
                }
            }
        }
    }

    private static void DisposeInvalidated(Task<TValue> invalidate)
    {
        TValue value = invalidate.GetAwaiter().GetResult();
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

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
            _queries.Writer.Complete();
            if (_cts is { })
            {
                try
                {
                    _cts.Cancel();
                }
                finally
                {
                    _cts.Dispose();
                }
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