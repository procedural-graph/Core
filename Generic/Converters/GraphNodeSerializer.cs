using System;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Provides an abstract base class for converting between graph node entities and their corresponding model
/// representations.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used to identify scene members. Must be a value type that implements 
/// <see cref="IEquatable{TKey}"/>.
/// </typeparam>
/// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
/// <typeparam name="TNode">
/// The type of the graph node to be converted. 
/// Must derive from <see cref="GraphEntity{TKey, TValue}"/>.
/// </typeparam>
/// <typeparam name="TModel">
/// The type of the model representation corresponding to the graph node. 
/// Must derive from <see cref="GraphEntity{TKey, TValue}.Model"/>.
/// </typeparam>
public abstract class GraphNodeSerializer<TKey, TValue, TNode, TModel> : GraphConverter<TNode>, IGraphConverter
    where TKey : struct, IEquatable<TKey>
    where TValue : class
    where TNode : class, IGraphNode
    where TModel : class
{
    /// <inheritdoc/>
    public override bool CanConvert([NotNullWhen(true)] object? obj)
    {
        return obj is TNode || obj is TModel;
    }

    /// <summary>
    /// Converts the specified <typeparamref name="TNode"/> to it's corresponding <typeparamref name="TModel"/> representation.
    /// </summary>
    /// <param name="node">
    /// The <typeparamref name="TNode"/> to convert to an <typeparamref name="TModel"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="graph">The Procedural Graph instance that the node belongs to. Cannot be <see langword="null"/>.</param>
    /// <returns>The model representation of the specified entity node.</returns>
    protected abstract TModel ToModel(TNode node, Graph<TKey, TValue> graph);

    object IGraphConverter.ToModel(IGraphNode node, IGraph graph)
    {
        TNode typedNode = node as TNode ?? throw new ArgumentException($"Must be of type {typeof(TNode)}.", nameof(node));
        Graph<TKey, TValue> typedGraph = graph as Graph<TKey, TValue> ?? throw new ArgumentException($"Must be of type {typeof(Graph<TKey, TValue>)}.", nameof(graph));
        return ToModel(typedNode, typedGraph);
    }
}
