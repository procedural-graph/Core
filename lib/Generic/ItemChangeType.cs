// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Specifies the type of change that has occurred to an item.
    /// </summary>
    public enum ItemChangeType : sbyte
    {
        /// <summary>
        /// Indicates that the item has been added.
        /// </summary>
        Added = +1,
        /// <summary>
        /// Indicates that the item has been removed.
        /// </summary>
        Removed = -1
    }
}
