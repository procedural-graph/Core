// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Specifies the possible states of an entity.
    /// </summary>
    [Flags]
    public enum EntityState : byte
    {
        /// <summary>
        /// Indicates that no options are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Indicates that the entity has started.
        /// </summary>
        Started = 1,
        /// <summary>
        /// Indicates that the entity has a pending task that is waiting to be completed.
        /// </summary>
        Pending = 1 << 1,
        /// <summary>
        /// Indicates that the entity is currently performing a task.
        /// </summary>
        Busy = 1 << 2,
        /// <summary>
        /// Indicates that the entity has been disposed.
        /// </summary>
        Dead = 1 << 7
    }
}