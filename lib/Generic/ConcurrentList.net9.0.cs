// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProceduralGraph.Generic
{
    internal partial class ConcurrentList<T>
    {
        private volatile ImmutableList<T> _items = [];

        public ConcurrentList()
        {
            _items = [];
        }

        public ConcurrentList(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);
            _items = [.. collection];
        }
    }
}
