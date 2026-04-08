using ProceduralGraph.Events;
using ProceduralGraph.Mathematics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace ProceduralGraph.Generic;

/// <summary>
/// Defines an interface for managing scene members within a scene graph.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public interface ISceneMemberManager<TSceneMember> : IEqualityComparer<TSceneMember?> where TSceneMember : class
{
    /// <summary>
    /// Gets the parent of the specified value in the hierarchy, if one exists.
    /// </summary>
    /// <param name="value">The value for which to retrieve the parent.</param>
    /// <returns>The parent of the specified value, or <see langword="null"/> if the value has no parent or is not found in the hierarchy.</returns>
    TSceneMember? GetParent(TSceneMember value);

    /// <summary>
    /// Retrieves the root element of the scene graph that contains the specified <typeparamref name="TSceneMember"/>.
    /// </summary>
    /// <remarks>If the provided <typeparamref name="TSceneMember"/> is already the root, it is returned as-is.</remarks>
    /// <param name="value">
    /// The <typeparamref name="TSceneMember"/> for which to locate the root element. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>The root element of the scene graph that contains the specified <typeparamref name="TSceneMember"/>.</returns>
    TSceneMember GetRoot(TSceneMember value);

    /// <summary>
    /// Retrieves the immediate child elements of the specified value.
    /// </summary>
    /// <param name="value">The value whose immediate children are to be returned. Cannot be <see langword="null"/>.</param>
    /// <returns>
    /// A read-only collection containing the immediate children of the specified value. Returns an empty collection
    /// if the value has no children.
    /// </returns>
    IReadOnlyCollection<TSceneMember> GetChildren(TSceneMember value);

    /// <summary>
    /// Removes the specified scene member from the scene and releases any associated resources.
    /// </summary>
    /// <param name="value">The scene member to remove. Cannot be <see langword="null"/>.</param>
    void Destroy(TSceneMember value);

    /// <summary>
    /// Creates a new child control associated with the specified parent scene member.
    /// </summary>
    /// <param name="parent">
    /// The parent scene member to which the new child control will be attached. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>A new control instance associated with the specified parent scene member.</returns>
    Control<TSceneMember> CreateChild(TSceneMember parent);

    /// <summary>
    /// Gets an asynchronous event that is triggered when a new scene member is spawned.
    /// </summary>
    AsyncEvent<TSceneMember> Spawned { get; }

    /// <summary>
    /// Gets the asynchronous event that is triggered when a scene member is destroyed.
    /// </summary>
    AsyncEvent<TSceneMember> Destroyed { get; }
}
