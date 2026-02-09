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
    /// Represents a graph entity that supports dynamic composition of components and child
    /// entities and serves as a proxy for a scene member, allowing it to be integrated into the graph structure.
    /// </summary>
    /// <inheritdoc/>
    public abstract class GenerativeProxyGraphEntity<TKey, TValue> : GenerativeGraphEntity<TKey, TValue>, IProxyGraphNode<TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        /// <inheritdoc/>
        public abstract TValue SceneMember { get; }
    }
}
