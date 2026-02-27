using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides a thread-safe, read-only collection that supports concurrent event notification when items are added or
/// removed.
/// </summary>
/// <typeparam name="TItem">The type of elements contained in the collection.</typeparam>
/// <typeparam name="TEnumerator">The type of enumerator used to iterate through the collection.</typeparam>
public abstract partial class ConcurrentCollection<TItem, TEnumerator> : ICollection<TItem> where TEnumerator : IEnumerator<TItem>
{
    /// <summary>
    /// Represents the error message indicating that a modification attempt was made on a completed collection.
    /// </summary>
    protected const string ModificationAfterCompletionError = "Cannot modify a completed collection.";

    private static readonly UnboundedChannelOptions _channelOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    private readonly Channel<ItemEventArgs<TItem>> _events = Channel.CreateUnbounded<ItemEventArgs<TItem>>(_channelOptions);

    /// <summary>
    /// Gets a channel reader that can be used to subscribe to collection change events.
    /// </summary>
    public ChannelReader<ItemEventArgs<TItem>> Events => _events.Reader;

    /// <inheritdoc/>
    public abstract int Count { get; }

    bool ICollection<TItem>.IsReadOnly => true;

    private volatile bool _completed;
    /// <summary>
    /// Gets a value indicating whether the collection has been marked as complete, meaning that it can no longer be modified.
    /// </summary>
    protected bool IsCompleted => _completed;

    /// <summary>
    /// Marks the event stream as complete, preventing any further events from being written.
    /// </summary>
    public void Complete()
    {
        if (_events.Writer.TryComplete())
        {
            _completed = true;
        }
    }

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
        _events.Writer.TryWrite(new ItemEventArgs<TItem>(item, changeType));
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