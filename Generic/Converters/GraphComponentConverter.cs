using System;
using System.Collections.Immutable;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Represents a base implementation of an entity converter that provides mechanisms for converting between, 
/// graph components and model representations within a graph structure.
/// </summary>
/// <typeparam name="TComponent"></typeparam>
/// <typeparam name="TModel">The type of the model representation used for serialization and deserialization. Must be a non-nullable reference type.</typeparam>
/// <typeparam name="TEntity">The type of graph entity this component should attach to. Must derive from <see cref="LifecycleGraphNode{TKey, TValue}"/>.</typeparam>
/// <typeparam name="TKey">
/// The type of the key used to identify scene members. Must be a value type that implements 
/// <see cref="IEquatable{TKey}"/>.
/// </typeparam>
/// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
public abstract class GraphComponentConverter<TComponent, TModel, TEntity, TValue, TKey> : GraphNodeSerializer<TKey, TValue, TComponent, TModel>, IGraphConverter
    where TComponent : GraphComponent<TKey, TValue>
    where TModel : class
    where TEntity : LifecycleGraphNode<TKey, TValue>
    where TKey : struct, IEquatable<TKey>
    where TValue : class
{
    private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TComponent), typeof(TModel)];
    /// <inheritdoc/>
    public override ImmutableArray<Type> SupportedTypes => _supportedTypes;

    /// <summary>
    /// Converts the specified <typeparamref name="TModel"/> to it's corresponding <typeparamref name="TComponent"/> representation.
    /// </summary>
    /// <param name="model">
    /// The <typeparamref name="TModel"/> to convert to an <typeparamref name="TComponent"/>. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="graph">The Procedural Graph instance that the node belongs to. Cannot be <see langword="null"/>.</param>
    /// <param name="entity">
    /// The parent entity to associate with the new component.
    /// </param>
    /// <returns>The entity representation of the specified model.</returns>
    protected abstract IGraphNode ToComponent(TModel model, Graph<TKey, TValue> graph, TEntity entity);

    IGraphNode IGraphConverter.ToGraph(object obj, IGraph graph, IGraphNode? parent)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(parent, nameof(parent));
#else
        if (parent is null)
        {
            throw new ArgumentNullException(nameof(parent));
        }
#endif
        TEntity typedEntity = parent as TEntity ?? throw new ArgumentException($"Must be of type {typeof(TEntity)}.", nameof(parent));
        TModel typedModel = obj as TModel ?? throw new ArgumentException($"Must be of type {typeof(TModel)}.", nameof(obj));
        Graph<TKey, TValue> typedGraph = graph as Graph<TKey, TValue> ?? throw new ArgumentException($"Must be of type {typeof(Graph<TKey, TValue>)}.", nameof(graph));
        return ToComponent(typedModel, typedGraph, typedEntity);
    }

    IGraphNode IGraphConverter.ToGraph(object sceneMember, IGraph root, object model, IGraphNode? parent)
    {
        throw new NotSupportedException($"Cannot create a component from a scene member.");
    }
}
