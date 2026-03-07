using System.Collections.Generic;

namespace ProceduralGraph.Generic;

/// <summary>
/// Defines methods to provide information and comparison operations for scene member objects identified by a key.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
public interface ISceneMemberInfoProvider<TSceneMember> : IEqualityComparer<TSceneMember?> where TSceneMember : class
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
}
