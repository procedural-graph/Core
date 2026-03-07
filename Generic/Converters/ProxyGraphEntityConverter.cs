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
/// <see cref="LifecycleGraphNode{TSceneMember}"/> and implement <see cref="IProxyGraphNode{TValue}"/>.
/// </typeparam>
/// <typeparam name="TSceneMember">
/// The engine-specific type of scene hierarchy member being converted. 
/// Must derive from <typeparamref name="TBaseSceneMember"/>.
/// </typeparam>
/// <typeparam name="TBaseSceneMember">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
public abstract class ProxyGraphEntityConverter<TEntity, TSceneMember, TBaseSceneMember> : GraphConverter<TEntity>, IGraphConverter
    where TEntity : LifecycleGraphNode<TBaseSceneMember>, IProxyGraphNode<TBaseSceneMember>
    where TSceneMember : class, TBaseSceneMember
    where TBaseSceneMember : class
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
