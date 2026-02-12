// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Represents an abstract component within a graph structure that is associated with a specific graph entity.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key used to identify scene members. Must be a value type that implements 
    /// <see cref="IEquatable{TKey}"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
    public abstract partial class GraphComponent<TKey, TValue> : IGraphNode
        where TKey : struct, IEquatable<TKey>
        where TValue : class
    {
        private static readonly ReadOnlyCollection<IGraphNode> _emptyDescendants;

        /// <summary>
        /// Gets the <see cref="GraphEntity{TKey, TValue}"/> associated with this component.
        /// </summary>
        public abstract GraphEntity<TKey, TValue> Entity { get; }
        IGraphNode? IGraphNode.Parent => Entity;

        /// <summary>
        /// Occurs when the state of the component has changed.
        /// </summary>
        public abstract event Action? StateChanged;

        static GraphComponent()
        {
            _emptyDescendants = new ReadOnlyCollection<IGraphNode>(Array.Empty<IGraphNode>());
        }

        ICollection<IGraphNode> IGraphNode.Descendants => _emptyDescendants;
    }
}
