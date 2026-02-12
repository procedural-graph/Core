// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;

namespace ProceduralGraph
{
    /// <summary>
    /// Represents a node within the procedural graph.
    /// </summary>
    public interface IGraphNode
    {
        /// <summary>
        /// Gets the parent node of this instance, if any.
        /// </summary>
        IGraphNode? Parent { get; }

        /// <summary>
        /// Gets the collection of descendant graphs in the hierarchy.
        /// </summary>
        ICollection<IGraphNode> Descendants { get; }
    }
}
