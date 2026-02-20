namespace ProceduralGraph.Collections
{
    /// <summary>
    /// Represents event data for a change to an item, including the affected item and the type of change that occurred.
    /// </summary>
    /// <typeparam name="T">The type of the item associated with the event.</typeparam>
    public readonly struct ItemEventArgs<T>
    {
        /// <summary>
        /// Gets the item affected by the change.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Gets the type of change that occurred to the item.
        /// </summary>
        public ItemChangeType ChangeType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemEventArgs{T}"/> structure with the specified item and change type.
        /// </summary>
        /// <param name="item">The item associated with the event. This parameter cannot be <see langword="null"/>.</param>
        /// <param name="changeType">The type of change that occurred to the item.</param>
        public ItemEventArgs(T item, ItemChangeType changeType)
        {
            Item = item;
            ChangeType = changeType;
        }
    }
}