using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProceduralGraph.Generic;

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
    /// <summary>
    /// Represents an abstract model that serves as a base for derived models, providing a unique identifier for the
    /// parent node.
    /// </summary>
    public abstract record Model
    {
        /// <summary>
        /// Gets the unique identifier of the parent node associated with this node.
        /// </summary>
        public Guid ParentID { get; init; }
    }

    /// <summary>
    /// Gets the <see cref="GraphEntity{TKey, TValue}"/> associated with this component.
    /// </summary>
    public abstract GraphEntity<TKey, TValue> Entity { get; }
    IGraphNode? IGraphNode.Parent => Entity;

    /// <summary>
    /// Occurs when the state of the component has changed.
    /// </summary>
    public abstract event Action? StateChanged;

    ICollection<IGraphNode> IGraphNode.Descendants => [];
}
