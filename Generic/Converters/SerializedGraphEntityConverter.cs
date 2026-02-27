using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Represents a base implementation of an entity converter that provides mechanisms for converting between, 
/// graph entities and model representations within a graph structure.
/// </summary>
/// <typeparam name="TEntity">The type of graph entity being converted. Must derive from <see cref="LifecycleGraphNode{TKey, TValue}"/>.</typeparam>
/// <typeparam name="TModel">The type of the model representation used for serialization and deserialization. Must be a reference type.</typeparam>
/// <typeparam name="TKey">
/// The type of the key used to identify scene members. Must be a value type that implements 
/// <see cref="IEquatable{TKey}"/>.
/// </typeparam>
/// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
public abstract class SerializedGraphEntityConverter<TEntity, TModel, TKey, TValue> : GraphConverter, IGraphConverter 
    where TEntity : LifecycleGraphNode<TKey, TValue>
    where TKey : struct, IEquatable<TKey>
    where TValue : class
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
    /// <param name="graph">The Procedural Graph instance that the node belongs to. Cannot be <see langword="null"/>.</param>
    /// <param name="parent">
    /// The parent graph node to associate with the new entity, 
    /// or <see langword="null"/> if the entity has no parent.
    /// </param>
    /// <returns>The entity representation of the specified model.</returns>
    protected abstract TEntity ToEntity(TModel model, Graph<TKey, TValue> graph, IGraphNode? parent = null);

    /// <summary>
    /// Converts the specified <typeparamref name="TEntity"/> to it's corresponding <typeparamref name="TModel"/> representation.
    /// </summary>
    /// <param name="node">
    /// The <typeparamref name="TEntity"/> to convert to an <typeparamref name="TModel"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="graph">The Procedural Graph instance that the node belongs to. Cannot be <see langword="null"/>.</param>
    /// <returns>The model representation of the specified entity node.</returns>
    protected abstract TModel ToModel(TEntity node, Graph<TKey, TValue> graph);

    IGraphNode IGraphConverter.ToGraph(object obj, IGraph graph, IGraphNode? parent)
    {
        Graph<TKey, TValue> typedGraph = graph as Graph<TKey, TValue> ?? throw new ArgumentException($"Must be of type {typeof(Graph<TKey, TValue>)}.", nameof(graph));
        TModel typedModel = obj as TModel ?? throw new ArgumentException($"Must be of type {typeof(TModel)}.", nameof(obj));
        return ToEntity(typedModel, typedGraph, parent);
    }

    object IGraphConverter.ToModel(IGraphNode node, IGraph graph)
    {
        Graph<TKey, TValue> typedGraph = graph as Graph<TKey, TValue> ?? throw new ArgumentException($"Must be of type {typeof(Graph<TKey, TValue>)}.", nameof(graph));
        TEntity typedNode = node as TEntity ?? throw new ArgumentException($"Must be of type {typeof(TEntity)}.", nameof(node));
        return ToModel(typedNode, typedGraph);
    }
}
