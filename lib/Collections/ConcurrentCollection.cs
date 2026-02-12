// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections
{
    /// <summary>
    /// Provides a thread-safe, read-only collection that supports concurrent event notification when items are added or
    /// removed.
    /// </summary>
    /// <typeparam name="TItem">The type of elements contained in the collection.</typeparam>
    /// <typeparam name="TEnumerator">The type of enumerator used to iterate through the collection.</typeparam>
    public abstract partial class ConcurrentCollection<TItem, TEnumerator> : ICollection<TItem> where TEnumerator : IEnumerator<TItem>
    {
        private static readonly BoundedChannelOptions _channelOptions;

        static ConcurrentCollection()
        {
            _channelOptions = new BoundedChannelOptions(capacity: 1024)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropOldest
            };
        }

        private readonly Channel<ItemEventArgs<TItem>> _events = Channel.CreateBounded<ItemEventArgs<TItem>>(_channelOptions);

        /// <summary>
        /// Gets a channel reader that can be used to subscribe to collection change events.
        /// </summary>
        public ChannelReader<ItemEventArgs<TItem>> Events => _events.Reader;

        /// <inheritdoc/>
        public abstract int Count { get; }

        bool ICollection<TItem>.IsReadOnly => true;

        /// <summary>
        /// Notifies subscribers that the collection has changed by raising a collection changed event for the specified
        /// item and change type.
        /// </summary>
        /// <param name="item">The item in the collection that was added, removed, or otherwise changed.</param>
        /// <param name="changeType">The type of change that occurred to the item. Specifies whether the item was added, removed, or updated.</param>
        /// <exception cref="InvalidOperationException">Thrown if the underlying event channel is closed or full and cannot accept new events.</exception>
        protected void RaiseCollectionChanged(TItem item, ItemChangeType changeType)
        {
            ItemEventArgs<TItem> eventArgs = CreateEventArgs(item, changeType);
            if (!_events.Writer.TryWrite(eventArgs))
            {
                throw new InvalidOperationException("Channel is closed or full.");
            }
        }

        /// <summary>
        /// Asynchronously raises a collection changed event for the specified item and change type.
        /// </summary>
        /// <param name="item">The item in the collection that was added, removed, or otherwise changed.</param>
        /// <param name="changeType">The type of change that occurred to the item. Specifies whether the item was added, removed, or updated.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected async ValueTask RaiseCollectionChangedAsync(TItem item, ItemChangeType changeType, CancellationToken cancellationToken = default)
        {
            ItemEventArgs<TItem> eventArgs = CreateEventArgs(item, changeType);
            ValueTask write = _events.Writer.WriteAsync(eventArgs, cancellationToken);
            await write.ConfigureAwait(false);
        }

        /// <summary>
        /// Throws an exception if the collection has been marked as completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the collection is completed.</exception>
        protected void ThrowIfCompleted()
        {
            if (_complete)
            {
                throw new InvalidOperationException("Collection is completed.");
            }
        }

        /// <inheritdoc/>
        public abstract bool Contains(TItem item);

        /// <inheritdoc/>
        public abstract void CopyTo(TItem[] array, int arrayIndex);

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
}