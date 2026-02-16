using System;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters
{
    /// <summary>
    /// Provides an abstract base class for converting between graph node entities and their corresponding model
    /// representations.
    /// </summary>
    /// <typeparam name="TNode">The type of the graph node to be converted. Must implement the <see cref="IGraphNode"/>.</typeparam>
    /// <typeparam name="TModel">The type of the model representation corresponding to the graph node.</typeparam>
    public abstract class GraphNodeSerializer<TNode, TModel> : GraphConverter<TNode>, IGraphConverter
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
        /// <param name="root">
        /// The asynchronous lifecycle host that manages the entity's lifecycle. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <returns>The model representation of the specified entity node.</returns>
        protected abstract TModel ToModel(TNode node, IGraph root);

        object IGraphConverter.ToModel(IGraphNode node, IGraph host)
        {
            TNode typedNode = node as TNode ?? throw new ArgumentException($"Must be of type {typeof(TNode)}.", nameof(node));
            return ToModel(typedNode, host);
        }
    }
}
