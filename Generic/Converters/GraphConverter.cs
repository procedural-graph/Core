using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters
{
    /// <inheritdoc/>
    /// <typeparam name="T">The type of graph node being converted. Must implement <see cref="IGraphNode"/>.</typeparam>
    public abstract class GraphConverter<T> : GraphConverter, IGraphConverter where T : IGraphNode
    {
        IGraphNode IGraphConverter.ToGraph(object sceneMember, IGraph root, object model, IGraphNode? parent)
        {
            throw new NotSupportedException($"{typeof(T).FullName} does not support serialization.");
        }

        object IGraphConverter.ToModel(IGraphNode node, IGraph root)
        {
            throw new NotSupportedException($"{typeof(T).FullName} does not support serialization.");
        }
    }

    /// <summary>
    /// Provides a base class for converting between object models and graph representations.
    /// </summary>
    public abstract class GraphConverter : IGraphConverter
    {
        /// <inheritdoc cref="IGraphConverter.SupportedTypes"/>
        public abstract ImmutableArray<Type> SupportedTypes { get; }
        IReadOnlyCollection<Type> IGraphConverter.SupportedTypes => SupportedTypes;

        /// <inheritdoc/>
        public abstract bool CanConvert([NotNullWhen(true)] object? obj);

        /// <inheritdoc/>
        public virtual int CompareTo(IGraphConverter? other) => 0;

        IGraphNode IGraphConverter.ToGraph(object obj, IGraph root, IGraphNode? parent)
        {
            throw new NotImplementedException();
        }

        IGraphNode IGraphConverter.ToGraph(object sceneMember, IGraph root, object model, IGraphNode? parent)
        {
            throw new NotImplementedException();
        }

        object IGraphConverter.ToModel(IGraphNode node, IGraph root)
        {
            throw new NotImplementedException();
        }
    }
}
