using System.Runtime.CompilerServices;

namespace ProceduralGraph.Collections
{
    public abstract partial class ConcurrentCollection<TItem, TEnumerator>
    {
        private volatile bool _complete;

        /// <include file='ConcurrentCollection.cs.xml' path='doc/members[@name="ConcurrentCollection"]/Complete/*'/>
        public void Complete()
        {
            bool completed = true;
            (_complete, completed) = (completed, _complete);
            if (completed)
            {
                return;
            }

            _events.Writer.Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ItemEventArgs<TItem> CreateEventArgs(TItem item, ItemChangeType changeType)
        {
            return new ItemEventArgs<TItem>(item, changeType);
        }
    }
}
