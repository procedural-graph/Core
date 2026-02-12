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

namespace ProceduralGraph.Collections;

public partial class ConcurrentGroupedCollection<TKey, TItem> : IReadOnlyDictionary<TKey, IReadOnlySet<TItem>>
{
    /// <inheritdoc/>
    public IReadOnlySet<TItem> this[TKey key] => _items[key];

    IEnumerable<IReadOnlySet<TItem>> IReadOnlyDictionary<TKey, IReadOnlySet<TItem>>.Values => _items.Values;

    /// <summary>
    /// Removes the items associated with the specified key from the collection.
    /// </summary>
    /// <param name="key">The key whose associated items are to be removed from the collection.</param>
    /// <param name="items">
    /// hen this method returns, contains a read-only set of items that were removed if the key was found; otherwise,
    /// an empty set.
    /// </param>
    /// <returns><see langword="true"/> if the key was found and its items were removed; otherwise, <see langword="false"/>.</returns>
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

    /// <inheritdoc/>
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
