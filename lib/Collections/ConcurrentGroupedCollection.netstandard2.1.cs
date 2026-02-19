using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace ProceduralGraph.Collections
{
    public partial class ConcurrentGroupedCollection<TKey, TItem> : IReadOnlyDictionary<TKey, IReadOnlyCollection<TItem>>
    {
        /// <inheritdoc/>
        public IReadOnlyCollection<TItem> this[TKey key] => _items[key];

        IEnumerable<IReadOnlyCollection<TItem>> IReadOnlyDictionary<TKey, IReadOnlyCollection<TItem>>.Values => _items.Values;

        /// <summary>
        /// Removes the items associated with the specified key from the collection.
        /// </summary>
        /// <param name="key">The key whose associated items are to be removed from the collection.</param>
        /// <param name="items">
        /// hen this method returns, contains a read-only collection of items that were removed if the key was found; otherwise,
        /// an empty collection.
        /// </param>
        /// <returns><see langword="true"/> if the key was found and its items were removed; otherwise, <see langword="false"/>.</returns>
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

        /// <inheritdoc/>
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
