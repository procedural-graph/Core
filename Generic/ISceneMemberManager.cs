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
    /// Gets the name of the specified scene member.
    /// </summary>
    /// <param name="value">The scene member for which to retrieve the name. Cannot be <see langword="null"/>.</param>
    /// <returns>The name of the specified scene member.</returns>
    string GetName(TSceneMember value);

    /// <summary>
    /// Sets the name of the specified scene member.
    /// </summary>
    /// <param name="value">The scene member whose name is to be set.</param>
    /// <param name="name">The new name to assign to the scene member.</param>
    void SetName(TSceneMember value, string name);

    /// <summary>
    /// Gets the position of the specified scene member.
    /// </summary>
    /// <param name="value">The scene member for which to retrieve the position. Cannot be <see langword="null"/>.</param>
    /// <returns>The position of the specified scene member.</returns>
    Double3 GetPosition(TSceneMember value);

    /// <summary>
    /// Sets the position of the specified scene member to the given coordinates.
    /// </summary>
    /// <param name="value">The scene member whose position is to be updated.</param>
    /// <param name="position">The new position for the scene member.</param>
    void SetPosition(TSceneMember value, in Double3 position);

    /// <summary>
    /// Gets the rotation of the specified scene member as a quaternion.
    /// </summary>
    /// <param name="value">The scene member for which to retrieve the rotation.</param>
    /// <returns>A quaternion representing the rotation of the specified scene member.</returns>
    Quaternion GetRotation(TSceneMember value);

    /// <summary>
    /// Sets the rotation of the specified scene member to the given orientation.
    /// </summary>
    /// <param name="value">The scene member whose rotation is to be set.</param>
    /// <param name="rotation">The new rotation to apply, represented as a quaternion.</param>
    void SetRotation(TSceneMember value, in Quaternion rotation);

    /// <summary>
    /// Gets the scale of the specified scene member as a three-dimensional vector.
    /// </summary>
    /// <param name="value">The scene member for which to retrieve the scale.</param>
    /// <returns>A <see cref="Vector3"/> representing the scale of the specified scene member.</returns>
    Vector3 GetScale(TSceneMember value);

    /// <summary>
    /// Sets the scale of the specified scene member to the given value.
    /// </summary>
    /// <param name="value">The scene member whose scale will be set. Cannot be null.</param>
    /// <param name="scale">The new scale to apply.</param>
    void SetScale(TSceneMember value, in Vector3 scale);

    /// <summary>
    /// Retrieves the transform associated with the specified scene member.
    /// </summary>
    /// <param name="value">The scene member for which to obtain the transform.</param>
    /// <returns>The transform corresponding to the specified scene member.</returns>
    Transform GetTransform(TSceneMember value);

    /// <summary>
    /// Sets the transform for the specified scene member.
    /// </summary>
    /// <param name="value">The scene member whose transform is to be set.</param>
    /// <param name="transform">The new transform to apply to the scene member.</param>
    void SetTransform(TSceneMember value, in Transform transform);

    /// <summary>
    /// Creates a new child scene member and attaches it to the specified parent.
    /// </summary>
    /// <param name="parent">The parent scene member to which the new child will be attached. Cannot be <see langword="null"/>.</param>
    /// <returns>The newly created child scene member.</returns>
    TSceneMember CreateChild(TSceneMember parent);

    /// <summary>
    /// Removes the specified scene member from the scene and releases any associated resources.
    /// </summary>
    /// <param name="value">The scene member to remove. Cannot be <see langword="null"/>.</param>
    void Destroy(TSceneMember value);
    
    /// <summary>
    /// Determines whether the specified scene member has been destroyed.
    /// </summary>
    /// <param name="value">The scene member to check.</param>
    /// <returns><see langword="true"/> if the scene member has been destroyed; otherwise, <see langword="false"/>.</returns>
    bool IsDestroyed([NotNullWhen(false)] TSceneMember? value);

    /// <summary>
    /// Subscribes to notifications when the transform of the specified scene member changes.
    /// </summary>
    /// <param name="value">The scene member to monitor for transform changes.</param>
    /// <param name="handler">The asynchronous event handler to invoke when the transform changes.</param>
    /// <inheritdoc cref="AsyncEventManager{TArgs}.Subscribe(AsyncEventHandler{TArgs})"/>
    AsyncEventSubscription<Transform> SubscribeTransformChanged(TSceneMember value, AsyncEventHandler<Transform> handler);
}
