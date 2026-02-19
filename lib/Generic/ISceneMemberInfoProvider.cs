using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Defines methods to provide information and comparison operations for scene member objects identified by a key.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key used to identify scene members. Must be a value type that implements 
    /// <see cref="IEquatable{TKey}"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The type of the scene member objects. Must be a reference type.</typeparam>
    public partial interface ISceneMemberInfoProvider<TKey, TValue> : IEqualityComparer<TValue?> where TKey : struct, IEquatable<TKey> where TValue : class
    {
        /// <summary>
        /// Attempts to retrieve the value associated with the specified key.
        /// </summary>
        /// <param name="key">The <paramref name="key"/> whose associated value is to be retrieved.</param>
        /// <param name="value">
        /// When this method returns, contains the <paramref name="value"/> associated with the specified 
        /// <paramref name="key"/>, if the key is found; otherwise, the default value for the type of the value parameter. 
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns><see langword="true"/> if the key was found and value was set; otherwise, <see langword="false"/>.</returns>
        bool TryFind(TKey key, [NotNullWhen(true)] out TValue? value);

        /// <summary>
        /// Retrieves the <typeparamref name="TKey"/> associated with the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <param name="value">
        /// The <typeparamref name="TValue"/> for which to retrieve the 
        /// corresponding <typeparamref name="TKey"/>.
        /// </param>
        /// <returns>The <typeparamref name="TKey"/> associated with the specified <typeparamref name="TValue"/>.</returns>
        TKey GetKey(TValue value);

        /// <summary>
        /// Gets the parent of the specified value in the hierarchy, if one exists.
        /// </summary>
        /// <param name="value">The value for which to retrieve the parent.</param>
        /// <returns>The parent of the specified value, or <see langword="null"/> if the value has no parent or is not found in the hierarchy.</returns>
        TValue? GetParent(TValue value);

        /// <summary>
        /// Retrieves the root element of the scene graph that contains the specified <typeparamref name="TValue"/>.
        /// </summary>
        /// <remarks>If the provided <typeparamref name="TValue"/> is already the root, it is returned as-is.</remarks>
        /// <param name="value">
        /// The <typeparamref name="TValue"/> for which to locate the root element. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <returns>The root element of the scene graph that contains the specified <typeparamref name="TValue"/>.</returns>
        TValue GetRoot(TValue value);

        /// <summary>
        /// Retrieves the immediate child elements of the specified value.
        /// </summary>
        /// <param name="value">The value whose immediate children are to be returned. Cannot be <see langword="null"/>.</param>
        /// <returns>
        /// A read-only collection containing the immediate children of the specified value. Returns an empty collection
        /// if the value has no children.
        /// </returns>
        IReadOnlyCollection<TValue> GetChildren(TValue value);
    }
}
