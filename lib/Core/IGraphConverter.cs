// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph
{
    /// <summary>
    /// Defines the contract for converting between engine objects and procedural graph entities.
    /// </summary>
    public interface IGraphConverter : IComparable<IGraphConverter>
    {
        /// <summary>
        /// Gets the collection of types supported by this instance.
        /// </summary>
        public IReadOnlyCollection<Type> SupportedTypes { get; }

        /// <summary>
        /// Determines whether the specified object can be converted by this converter.
        /// </summary>
        /// <param name="obj">The object to check for compatibility.</param>
        /// <returns><see langword="true"/> if the object can be converted; otherwise, <see langword="false"/>.</returns>
        bool CanConvert([NotNullWhen(true)] object? obj);

        /// <summary>
        /// Converts the specified object into a procedural graph node.
        /// </summary>
        /// <param name="obj">The source object to convert.</param>
        /// <param name="root">The manager handling the creation and disposal of procedural entities.</param>
        /// <param name="parent">The parent entity in the graph hierarchy, if applicable.</param>
        /// <returns>The converted <see cref="IGraphNode"/>.</returns>
        IGraphNode ToGraph(object obj, IGraph root, IGraphNode? parent = default);

        /// <summary>
        /// Converts the specified scene member into a procedural graph node using the provided model for additional context.
        /// </summary>
        /// <param name="sceneMember">The scene member to convert into a procedural graph node.</param>
        /// <param name="root">The manager handling the creation and disposal of procedural entities.</param>
        /// <param name="model">The model object to convert into a procedural graph node.</param>
        /// <param name="parent">The parent entity in the graph hierarchy, if applicable.</param>
        IGraphNode ToGraph(object sceneMember, IGraph root, object model, IGraphNode? parent = default);

        /// <summary>
        /// Converts a procedural graph node into it's model representation.
        /// </summary>
        /// <param name="node">The <see cref="IGraphNode"/> to transform.</param>
        /// <param name="root">The manager handling the creation and disposal of procedural graph nodes.</param>
        /// <returns>The converted model object.</returns>
        object ToModel(IGraphNode node, IGraph root);
    }
}

