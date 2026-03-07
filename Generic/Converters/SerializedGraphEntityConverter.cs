using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Represents a base implementation of an entity converter that provides mechanisms for converting between, 
/// graph entities and model representations within a graph structure.
/// </summary>
/// <inheritdoc cref="LifecycleGraphNode{TSceneMember}"/>
/// <typeparam name="TEntity">The type of graph entity being converted. Must derive from <see cref="LifecycleGraphNode{TSceneMember}"/>.</typeparam>
/// <typeparam name="TModel">The type of the model representation used for serialization and deserialization. Must be a reference type.</typeparam>
/// <typeparam name="TSceneMember"/>
public abstract class SerializedGraphEntityConverter<TEntity, TModel, TSceneMember> : GraphConverter, IGraphConverter 
    where TEntity : LifecycleGraphNode<TSceneMember>
    where TSceneMember : class
    where TModel : class
{
    private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TEntity), typeof(TModel)];
    /// <inheritdoc/>
    public override ImmutableArray<Type> SupportedTypes => _supportedTypes;

    /// <inheritdoc/>
    public override bool CanConvert([NotNullWhen(true)] object? obj)
    {
        return obj is TEntity || obj is TModel;
    }

    /// <summary>
    /// Converts the specified <typeparamref name="TModel"/> to it's corresponding <typeparamref name="TEntity"/> representation.
    /// </summary>
    /// <param name="model">
    /// The <typeparamref name="TModel"/> to convert to an <typeparamref name="TEntity"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="parent">
    /// The parent graph node to associate with the new entity, 
    /// or <see langword="null"/> if the entity has no parent.
    /// </param>
    /// <returns>The entity representation of the specified model.</returns>
    protected abstract TEntity ToEntity(TModel model, IGraphNode? parent = null);

    /// <summary>
    /// Converts the specified <typeparamref name="TEntity"/> to it's corresponding <typeparamref name="TModel"/> representation.
    /// </summary>
    /// <param name="node">
    /// The <typeparamref name="TEntity"/> to convert to an <typeparamref name="TModel"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <returns>The model representation of the specified entity node.</returns>
    protected abstract TModel ToModel(TEntity node);

    IGraphNode IGraphConverter.ToGraph(object obj, IGraphNode? parent)
    {
        TModel typedModel = obj as TModel ?? throw new ArgumentException($"Must be of type {typeof(TModel)}.", nameof(obj));
        return ToEntity(typedModel, parent);
    }

    object IGraphConverter.ToModel(IGraphNode node)
    {
        TEntity typedNode = node as TEntity ?? throw new ArgumentException($"Must be of type {typeof(TEntity)}.", nameof(node));
        return ToModel(typedNode);
    }
}
