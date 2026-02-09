// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    internal partial class ConcurrentGroupedCollection<TKey, TItem> : ConcurrentCollection<TItem>, ICollection<TItem> where TKey : notnull
    {
        public struct Enumerator : IEnumerator<TItem>
        {
            private readonly IEnumerator<ImmutableHashSet<TItem>> _setEnumerator;
            private ImmutableHashSet<TItem>.Enumerator _itemEnumerator;
            private bool _initialized;
        
            internal Enumerator(IEnumerator<ImmutableHashSet<TItem>> setEnumerator)
            {
                _setEnumerator = setEnumerator;
                _itemEnumerator = default;
                _initialized = false;
            }

            public readonly TItem Current => _itemEnumerator.Current;
            readonly object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (_initialized && _itemEnumerator.MoveNext())
                {
                    return true;
                }

                while (_setEnumerator.MoveNext())
                {
                    _itemEnumerator = _setEnumerator.Current.GetEnumerator();
                    _initialized = true;

                    if (_itemEnumerator.MoveNext())
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                _setEnumerator.Reset();
                _itemEnumerator = default;
                _initialized = false;
            }

            public void Dispose()
            {
                _setEnumerator.Dispose();
                _itemEnumerator.Dispose();
            }
        }

        private readonly Func<TItem, TKey> _keySelector;
        private readonly ConcurrentDictionary<TKey, ImmutableHashSet<TItem>> _items;

        private int _count;
        public int Count => _count;

        public IEnumerable<TKey> Keys => _items.Keys;

        bool ICollection<TItem>.IsReadOnly => false;

        public ConcurrentGroupedCollection(Func<TItem, TKey> keySelector)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(keySelector, nameof(keySelector));
#else
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
#endif

            _items = new ConcurrentDictionary<TKey, ImmutableHashSet<TItem>>();
            _keySelector = keySelector;
        }

        public bool Add(TItem item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#endif

            if (TryAdd(item))
            {
                RaiseCollectionChanged(item, ItemChangeType.Added);
                return true;
            }

            return false;
        }

        public async ValueTask<bool> AddAsync(TItem item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#endif

            if (TryAdd(item))
            {
                ValueTask write = RaiseCollectionChangedAsync(item, ItemChangeType.Added, cancellationToken);
                await write.ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_items.Values.GetEnumerator());
        }

        public bool Remove(TItem item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#endif
    
            if (TryRemove(item))
            {
                RaiseCollectionChanged(item, ItemChangeType.Removed);
                return true;
            }

            return false;
        }

        public bool RemoveAsync(TKey key, out IReadOnlyCollection<TItem> items, CancellationToken cancellationToken = default)
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

#if NET8_0_OR_GREATER
            items = [];
#else
            items = ImmutableHashSet<TItem>.Empty;
#endif
            return false;
        }

        public async ValueTask<bool> RemoveAsync(TItem item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#endif

            if (TryRemove(item))
            {
                ValueTask write = RaiseCollectionChangedAsync(item, ItemChangeType.Removed, cancellationToken);
                await write.ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }

        public bool Contains(TItem item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }
#endif

            TKey key = _keySelector(item);

            if (_items.TryGetValue(key, out ImmutableHashSet<TItem>? items))
            {
                return items.Contains(item);
            }

            return false;
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(array, nameof(array));
#else
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
#endif

            using Enumerator enumerator = GetEnumerator();
            while (arrayIndex < array.Length && enumerator.MoveNext())
            {
                array[arrayIndex++] = enumerator.Current;
            }
        }

        private bool TryAdd(TItem item)
        {
            CheckCompleted();

            TKey key = _keySelector(item);

            while (true)
            {
                if (_items.TryGetValue(key, out ImmutableHashSet<TItem>? currentValue))
                {
                    ImmutableHashSet<TItem> computedValue = currentValue.Add(item);

                    if (ReferenceEquals(computedValue, currentValue))
                    {
                        return false;
                    }

                    if (_items.TryUpdate(key, computedValue, currentValue))
                    {
                        break;
                    }

                    continue;
                }

#if NET5_0_OR_GREATER
                ImmutableHashSet<TItem> items = [];
#else
                ImmutableHashSet<TItem> items = ImmutableHashSet.Create(item);
#endif
                if (_items.TryAdd(key, items))
                {
                    break;
                }
            }

            Interlocked.Increment(ref _count);
            return true;
        }

        private bool TryRemove(TItem item)
        {
            CheckCompleted();

            TKey key = _keySelector(item);

            ImmutableHashSet<TItem>? currentValue;
            ImmutableHashSet<TItem> computedValue;
            do
            {
                if (!_items.TryGetValue(key, out currentValue))
                {
                    return false;
                }

                computedValue = currentValue.Remove(item);

                if (ReferenceEquals(computedValue, currentValue))
                {
                    return false;
                }
            }
            while (!_items.TryUpdate(key, computedValue, currentValue));

            if (computedValue.IsEmpty)
            {
                TryRemoveAtomic(key, computedValue);
            }

            Interlocked.Decrement(ref _count);
            return true;
        }

        private bool TryRemoveAtomic(TKey key, ImmutableHashSet<TItem> expectedValue)
        {
            var kvp = new KeyValuePair<TKey, ImmutableHashSet<TItem>>(key, expectedValue);
            return ((ICollection<KeyValuePair<TKey, ImmutableHashSet<TItem>>>)_items).Remove(kvp);
        }

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<TItem>.Add(TItem item)
        {
            Add(item);
        }

        void ICollection<TItem>.Clear()
        {
            foreach (var key in _items.Keys)
            {
                if (!_items.TryRemove(key, out ImmutableHashSet<TItem>? items) || items.IsEmpty)
                {
                    continue;
                }

                using ImmutableHashSet<TItem>.Enumerator enumerator = items.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Interlocked.Decrement(ref _count);
                    RaiseCollectionChanged(enumerator.Current, ItemChangeType.Removed);
                }
            }
        }
    }
}
