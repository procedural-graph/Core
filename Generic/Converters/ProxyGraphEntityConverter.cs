using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Represents a base implementation of an entity converter that provides mechanisms for converting between scene members 
/// and graph entity representations within a graph structure.
/// </summary>
/// <typeparam name="TEntity">
/// The type of graph entity being converted. Must derive from 
/// <see cref="LifecycleGraphNode{TKey, TValue}"/> and implement <see cref="IProxyGraphNode{TValue}"/>.
/// </typeparam>
/// <typeparam name="TSceneMember">
/// The engine-specific type of scene hierarchy member being converted. 
/// Must derive from <typeparamref name="TValue"/>.
/// </typeparam>
/// <typeparam name="TKey">
/// The type of the key used to identify scene members. Must be a value type that implements 
/// <see cref="IEquatable{TKey}"/>.
/// </typeparam>
/// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
public abstract class ProxyGraphEntityConverter<TEntity, TSceneMember, TKey, TValue> : GraphConverter<TEntity>, IGraphConverter
    where TEntity : LifecycleGraphNode<TKey, TValue>, IProxyGraphNode<TValue>
    where TSceneMember : class, TValue
    where TKey : struct, IEquatable<TKey>
    where TValue : class
{
    private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TSceneMember), typeof(TEntity)];
    /// <inheritdoc/>
    public override ImmutableArray<Type> SupportedTypes => _supportedTypes;

    /// <inheritdoc/>
    public override bool CanConvert([NotNullWhen(true)] object? obj) => obj is TEntity;

    /// <summary>
    /// Converts the specified <typeparamref name="TSceneMember"/> to it's corresponding <typeparamref name="TEntity"/> representation.
    /// </summary>
    /// <param name="sceneMember">
    /// The <typeparamref name="TSceneMember"/> to convert to an <typeparamref name="TEntity"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="parent">
    /// The parent graph node to associate with the new entity, 
    /// or <see langword="null"/> if the entity has no parent.
    /// </param>
    /// <returns>The entity representation of the specified scene member.</returns>
    protected abstract TEntity ToEntity(TSceneMember sceneMember, IGraphNode? parent = null);

    IGraphNode IGraphConverter.ToGraph(object obj, IGraphNode? parent)
    {
        TSceneMember typedSceneMember = obj as TSceneMember ?? throw new ArgumentException($"Must be of type {typeof(TSceneMember)}.", nameof(obj));
        return ToEntity(typedSceneMember, parent);

    }
}
