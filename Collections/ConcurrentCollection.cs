using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using ProceduralGraph.Events;

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides a thread-safe, read-only collection that supports concurrent event notification when items are added or
/// removed.
/// </summary>
/// <typeparam name="TItem">The type of elements contained in the collection.</typeparam>
/// <typeparam name="TEnumerator">The type of enumerator used to iterate through the collection.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ConcurrentCollection{TItem, TEnumerator}"/> class.
/// </remarks>
/// <returns/>
/// <inheritdoc cref="AsyncEventPublisher.Create{TArgs}(UnboundedChannelOptions, ILogger)"/>
public abstract class ConcurrentCollection<TItem, TEnumerator>(ILogger logger) : Disposable, ICollection<TItem> where TEnumerator : IEnumerator<TItem>
{
    private static readonly UnboundedChannelOptions _channelOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    /// <inheritdoc/>
    public abstract int Count { get; }

    private readonly AsyncEventPublisher<ItemEventArgs<TItem>> _collectionChanged = AsyncEventPublisher.Create<ItemEventArgs<TItem>>(_channelOptions, logger);
    /// <summary>
    /// Gets an asynchronous event that subscribers can listen to for notifications when items
    /// are added or removed from the collection.
    /// </summary>
    public AsyncEvent<ItemEventArgs<TItem>> CollectionChanged => _collectionChanged.Event;

    bool ICollection<TItem>.IsReadOnly => true;

    /// <summary>
    /// Notifies subscribers that the collection has changed by raising a collection changed event for the specified
    /// item and change type.
    /// </summary>
    /// <param name="item">The item in the collection that was added, removed, or otherwise changed.</param>
    /// <param name="changeType">The type of change that occurred to the item. Specifies whether the item was added, removed, or updated.</param>
    /// <exception cref="InvalidOperationException">Thrown if the underlying event channel is closed or full and cannot accept new events.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RaiseCollectionChanged(TItem item, ItemChangeType changeType)
    {
        ItemEventArgs<TItem> args = new(item, changeType);
        _collectionChanged.Publish(args);
    }

    /// <inheritdoc/>
    public abstract bool Contains(TItem item);

    /// <returns>The number of items that were copied to the destination array.</returns>
    /// <inheritdoc cref="ICollection{T}.CopyTo(T[], int)"/>
    public abstract int CopyTo(TItem[] array, int arrayIndex);

    void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    void ICollection<TItem>.Add(TItem item)
    {
        throw new NotSupportedException("Collection is read-only.");
    }

    void ICollection<TItem>.Clear()
    {
        throw new NotSupportedException("Collection is read-only.");
    }

    bool ICollection<TItem>.Remove(TItem item)
    {
        throw new NotSupportedException("Collection is read-only.");
    }

    /// <inheritdoc cref="IEnumerable{TItem}.GetEnumerator"/>
    public abstract TEnumerator GetEnumerator();
    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}