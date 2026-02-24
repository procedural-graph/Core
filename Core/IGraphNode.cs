using System.Collections.Generic;

namespace ProceduralGraph;

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
