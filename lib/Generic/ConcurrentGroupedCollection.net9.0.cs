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

namespace ProceduralGraph.Generic;

internal partial class ConcurrentGroupedCollection<TKey, TItem> : IReadOnlyDictionary<TKey, IReadOnlySet<TItem>>
{
    public IReadOnlySet<TItem> this[TKey key] => _items[key];

    IEnumerable<IReadOnlySet<TItem>> IReadOnlyDictionary<TKey, IReadOnlySet<TItem>>.Values => _items.Values;

    public bool Remove(TKey key, out IReadOnlySet<TItem> items)
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

    public bool TryGetValue(TKey key, out IReadOnlySet<TItem> value)
    {
        if (_items.TryGetValue(key, out ImmutableHashSet<TItem>? result))
        {
            value = result;
            return true;
        }

        value = ImmutableHashSet<TItem>.Empty;
        return false;
    }

    IEnumerator<KeyValuePair<TKey, IReadOnlySet<TItem>>> IEnumerable<KeyValuePair<TKey, IReadOnlySet<TItem>>>.GetEnumerator()
    {
        return (IEnumerator<KeyValuePair<TKey, IReadOnlySet<TItem>>>)_items.GetEnumerator();
    }
}
