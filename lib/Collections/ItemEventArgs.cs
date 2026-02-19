namespace ProceduralGraph.Collections
{
    public readonly struct ItemEventArgs<T>
    {
        public T Item { get; }

        public ItemChangeType ChangeType { get; }

        public ItemEventArgs(T item, ItemChangeType changeType)
        {
            Item = item;
            ChangeType = changeType;
        }
    }
}