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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic
{
    internal partial class ConcurrentList<T> : ConcurrentCollection<T>, ICollection<T>, IReadOnlyList<T>
    {
        public int Count => _items.Count;

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get => _items[index];
            set => SetItem(index, value);
        }

        public void Add(T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            CheckCompleted();

            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;
                ImmutableList<T> updated = original.Add(item);

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(item, ItemChangeType.Added);
                    break;
                }
                spinner.SpinOnce();
            }
        }

        public async ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            CheckCompleted();

            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;
                ImmutableList<T> updated = original.Add(item);

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(item, ItemChangeType.Added);
                    break;
                }
                spinner.SpinOnce();
            }
        }

        public bool Remove(T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            CheckCompleted();

            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;
                ImmutableList<T> updated = original.Remove(item);

                if (original == updated)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(item, ItemChangeType.Removed);
                    return true;
                }
                spinner.SpinOnce();
            }
        }

        public async ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            CheckCompleted();

            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;
                ImmutableList<T> updated = original.Remove(item);

                if (original == updated)
                {
                    return false;
                }

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(item, ItemChangeType.Removed);
                    return true;
                }
                spinner.SpinOnce();
            }
        }

        public void RemoveAt(int index)
        {
            CheckCompleted();
            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;

                if (index < 0 || index >= original.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                T itemToRemove = original[index];
                ImmutableList<T> updated = original.RemoveAt(index);

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(itemToRemove, ItemChangeType.Removed);
                    break;
                }
                spinner.SpinOnce();
            }
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void Clear()
        {
            CheckCompleted();
            while (true)
            {
                ImmutableList<T> original = _items;
                if (original.IsEmpty) return;

                if (Interlocked.CompareExchange(ref _items, ImmutableList<T>.Empty, original) == original)
                {
                    foreach (var item in original)
                    {
                        RaiseCollectionChanged(item, ItemChangeType.Removed);
                    }
                    return;
                }
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public ImmutableList<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        private void SetItem(int index, T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            CheckCompleted();

            SpinWait spinner = default;
            while (true)
            {
                ImmutableList<T> original = _items;

                if (index < 0 || index >= original.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                T oldItem = original[index];

                if (EqualityComparer<T>.Default.Equals(oldItem, item))
                {
                    return;
                }

                ImmutableList<T> updated = original.SetItem(index, item);

                if (Interlocked.CompareExchange(ref _items, updated, original) == original)
                {
                    RaiseCollectionChanged(oldItem, ItemChangeType.Removed);
                    RaiseCollectionChanged(item, ItemChangeType.Added);
                    break;
                }
                spinner.SpinOnce();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}