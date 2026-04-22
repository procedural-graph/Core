using ProceduralGraph.Mathematics;
using System.Collections.Generic;

namespace ProceduralGraph;

/// <summary>
/// Represents a member of a scene, which can have a parent, children, and a transform.
/// </summary>
public interface ISceneMember
{
    /// <summary>
    /// Gets or sets the parent object associated with this scene member.
    /// </summary>
    object? Parent { get; set; }

    /// <summary>
    /// Gets the underlying instance associated with this scene member.
    /// </summary>
    object Instance { get; }

    /// <summary>
    /// Gets or sets the name of this scene member.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the transformation matrix that defines the position, rotation, and scale of the object in world space.
    /// </summary>
    Transform Transform { get; set; }

    /// <summary>
    /// Gets the bounding box that defines the spatial limits of the object.
    /// </summary>
    BoundingBox Bounds { get; }

    /// <summary>
    /// Gets a read-only collection of scene members that are descendants of the current instance.
    /// </summary>
    IReadOnlyCollection<object> Children { get; }
}
