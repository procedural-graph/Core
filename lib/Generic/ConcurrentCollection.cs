// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    internal abstract partial class ConcurrentCollection<T>
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

        private readonly Channel<ItemEventArgs<T>> _events = Channel.CreateBounded<ItemEventArgs<T>>(_channelOptions);

        public ChannelReader<ItemEventArgs<T>> Events => _events.Reader;

        protected void RaiseCollectionChanged(T item, ItemChangeType changeType)
        {
            ItemEventArgs<T> eventArgs = CreateEventArgs(item, changeType);
            if (!_events.Writer.TryWrite(eventArgs))
            {
                throw new InvalidOperationException("Channel is closed or full.");
            }
        }

        protected async ValueTask RaiseCollectionChangedAsync(T item, ItemChangeType changeType, CancellationToken cancellationToken = default)
        {
            ItemEventArgs<T> eventArgs = CreateEventArgs(item, changeType);
            ValueTask write = _events.Writer.WriteAsync(eventArgs, cancellationToken);
            await write.ConfigureAwait(false);
        }

        protected void CheckCompleted()
        {
            if (_complete)
            {
                throw new InvalidOperationException("Collection is completed.");
            }
        }
    }
}