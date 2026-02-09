// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ProceduralGraph.Generic
{
    internal partial class ConcurrentGroupedCollection<TKey, TItem> : IReadOnlyDictionary<TKey, IReadOnlyCollection<TItem>>
    {
        public IReadOnlyCollection<TItem> this[TKey key] => _items[key];

        IEnumerable<IReadOnlyCollection<TItem>> IReadOnlyDictionary<TKey, IReadOnlyCollection<TItem>>.Values => _items.Values;

        public bool Remove(TKey key, out IReadOnlyCollection<TItem> items)
        {
            if (_items.TryRemove(key, out ImmutableHashSet<TItem>? value))
            {
                items = value;

                foreach (TItem item in value)
                {
                    Interlocked.Decrement(ref _count);
                    RaiseCollectionChanged(item, ItemChangeType.Removed);
                }

                return true;
            }

            items = ImmutableHashSet<TItem>.Empty;

            return false;
        }

        public bool TryGetValue(TKey key, out IReadOnlyCollection<TItem> value)
        {
            if (_items.TryGetValue(key, out ImmutableHashSet<TItem>? result))
            {
                value = result;
                return true;
            }

            value = ImmutableHashSet<TItem>.Empty;
            return false;
        }

        IEnumerator<KeyValuePair<TKey, IReadOnlyCollection<TItem>>> IEnumerable<KeyValuePair<TKey, IReadOnlyCollection<TItem>>>.GetEnumerator()
        {
            return (IEnumerator<KeyValuePair<TKey, IReadOnlyCollection<TItem>>>)_items.GetEnumerator();
        }
    }
}
