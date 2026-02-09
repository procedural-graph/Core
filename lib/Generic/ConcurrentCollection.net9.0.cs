// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProceduralGraph.Generic;

internal abstract partial class ConcurrentCollection<T>
{
	private bool _complete;

    public void Complete()
	{
		if (Interlocked.Exchange(ref _complete, true))
		{
			return;
		}

		_events.Writer.Complete();
	}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemEventArgs<T> CreateEventArgs(T item, ItemChangeType changeType) => new(item, changeType);
}
