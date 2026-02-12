// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
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