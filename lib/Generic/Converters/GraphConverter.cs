// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters
{
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

        IGraphNode IGraphConverter.ToGraph(object obj, IAsyncLifecycle host, IGraphNode? parent)
        {
            throw new NotSupportedException($"The {GetType().Name} does not support converting to a graph representation.");
        }

        object IGraphConverter.ToModel(IGraphNode node, IAsyncLifecycle host)
        {
            throw new NotSupportedException($"The {GetType().Name} does not support converting to a model representation.");
        }
    }
}
