using System.Runtime.CompilerServices;
using System.Threading;

namespace ProceduralGraph.Collections;

public abstract partial class ConcurrentCollection<TItem, TEnumerator>
{
	private bool _complete;

    /// <include file='ConcurrentCollection.cs.xml' path='doc/members[@name="ConcurrentCollection"]/Complete/*'/>
    public void Complete()
	{
		if (Interlocked.Exchange(ref _complete, true))
		{
			return;
		}

		_events.Writer.Complete();
	}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemEventArgs<TItem> CreateEventArgs(TItem item, ItemChangeType changeType) => new(item, changeType);
}
