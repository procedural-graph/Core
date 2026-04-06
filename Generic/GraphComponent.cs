using System;
using System.Collections.Generic;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents an abstract component within a graph structure that is associated with a specific graph entity.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public abstract class GraphComponent<TSceneMember> : IGraphNode where TSceneMember : class
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
    /// Gets the <see cref="GraphEntity{TValue}"/> associated with this component.
    /// </summary>
    public abstract GraphEntity<TSceneMember> Entity { get; }
    IGraphNode? IGraphNode.Parent => Entity;

    /// <summary>
    /// Occurs when the state of the component has changed.
    /// </summary>
    public abstract event Action? StateChanged;

    ICollection<IGraphNode> IGraphNode.Descendants => [];
}
