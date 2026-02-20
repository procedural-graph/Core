using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections
{
    /// <summary>
    /// Represents a thread-safe, mutable list that supports concurrent add, remove, and update operations.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the list.</typeparam>
    public partial class ConcurrentList<T> : ConcurrentCollection<T, ImmutableList<T>.Enumerator>, ICollection<T>, IList<T>
    {
        private volatile ImmutableList<T> _items;

        /// <inheritdoc/>
        public override int Count => _items.Count;

        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that is empty.
        /// </summary>
        public ConcurrentList()
        {
#if NET8_0_OR_GREATER
            _items = [];
#else
            _items = ImmutableList<T>.Empty;
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that contains elements copied from the specified
        /// collection.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list. Cannot be <see langword="null"/>.</param>
        public ConcurrentList(IEnumerable<T> collection)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(collection, nameof(collection));
#else
            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
#endif
#if NET8_0_OR_GREATER
            _items = [.. collection];
#else
            _items = ImmutableList.CreateRange(collection);
#endif
        }

        /// <inheritdoc/>
        public T this[int index]
        {
            get => _items[index];
            set => SetItem(index, value);
        }


        /// <inheritdoc/>
        public void Add(T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            ThrowIfCompleted();

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

        /// <summary>
        /// Asynchronously adds the specified item to the collection.
        /// </summary>
        /// <param name="item">The item to add to the collection. Cannot be <see langword="null"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the add operation.</param>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous add operation.</returns>
        public async ValueTask AddAsync(T item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            ThrowIfCompleted();

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

        /// <inheritdoc/>
        public bool Remove(T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            ThrowIfCompleted();

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

        /// <summary>
        /// Asynchronously removes the specified item from the collection, if it exists.
        /// </summary>
        /// <param name="item">The item to remove from the collection. Cannot be <see langword="null"/>.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the remove operation.</param>
        /// <returns>
        /// A <see cref="ValueTask"/> that represents the asynchronous remove operation. The result is <see langword="true"/> 
        /// if the item found and was successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> RemoveAsync(T item, CancellationToken cancellationToken = default)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            ThrowIfCompleted();

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

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            ThrowIfCompleted();
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

        /// <inheritdoc/>
        public override bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            ThrowIfCompleted();
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

        /// <inheritdoc/>
        public override void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        private void SetItem(int index, T item)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(item, nameof(item));
#else
            if (item is null) throw new ArgumentNullException(nameof(item));
#endif
            ThrowIfCompleted();

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

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            ImmutableList<T> oldValue = _items;
            ImmutableList<T> newValue;
            SpinWait spinner = default;
            while (true)
            {
                newValue = oldValue.Insert(index, item);
                ImmutableList<T> currentValue = Interlocked.CompareExchange(ref _items, newValue, oldValue);
                if (currentValue == oldValue)
                {
                    RaiseCollectionChanged(item, ItemChangeType.Added);
                    break;
                }
                oldValue = currentValue;
                spinner.SpinOnce();
            }
        }

        /// <inheritdoc/>
        public override ImmutableList<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}