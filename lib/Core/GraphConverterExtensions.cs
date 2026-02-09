// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph
{
    /// <summary>
    /// Provides extension methods for converting between graph models, graph nodes, and scene objects using an 
    /// <see cref="IGraphConverter"/>.
    /// </summary>
    public static class GraphConverterExtensions
    {
        /// <summary>
        /// Attempts to convert the specified object to a graph node using the provided converter.
        /// </summary>
        /// <param name="provider">The graph converter provider used to perform the conversion.</param>
        /// <param name="obj">The object to convert.</param>
        /// <param name="host">The graph host that provides context for the conversion.</param>
        /// <param name="node">
        /// When this method returns, contains the resulting graph node if the conversion succeeded; 
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <param name="parent">The optional parent node.</param>
        /// <returns><see langword="true"/> if the scene object was successfully converted; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert(
            this IGraphConverterProvider provider,
            [NotNullWhen(true)] object? obj,
            IAsyncLifecycle host,
            [NotNullWhen(true)] out IGraphNode? node,
            IGraphNode? parent = default)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (obj is { } && provider.TryFind(obj, out IGraphConverter? converter))
            {
                node = converter.ToGraph(obj, host, parent);
                return true;
            }

            node = default;
            return false;
        }

        /// <summary>
        /// Attempts to convert the specified object to a graph node of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of graph node to convert to. Must implement <see cref="IGraphNode"/>.</typeparam>
        /// <param name="provider">The graph converter provider used to perform the conversion.</param>
        /// <param name="obj">The object to convert.</param>
        /// <param name="host">The graph host context.</param>
        /// <param name="node">
        /// When this method returns, contains the converted graph node of type <typeparamref name="T"/> if successful;
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <param name="parent">The optional parent node.</param>
        /// <returns><see langword="true"/> if conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert<T>(
            this IGraphConverterProvider provider,
            [NotNullWhen(true)] object? obj,
            IAsyncLifecycle host,
            [NotNullWhen(true)] out T? node,
            IGraphNode? parent = default)
            where T : class, IGraphNode
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (obj is { } && provider.TryFind(obj, out IGraphConverter? converter))
            {
                node = converter.ToGraph(obj, host, parent) as T;
                return node is { };
            }

            node = default;
            return false;
        }

        /// <summary>
        /// Attempts to convert the specified graph node it's model representation.
        /// </summary>
        /// <param name="provider">The graph converter provider used to perform the conversion.</param>
        /// <param name="entity">The graph node to convert.</param>
        /// <param name="host">The graph host context.</param>
        /// <param name="result">
        /// When this method returns, contains the resulting object if the conversion succeeded; 
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> if the node was successfully converted to a model; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert(
            this IGraphConverterProvider provider,
            [NotNullWhen(true)] IGraphNode? entity,
            IAsyncLifecycle host,
            [NotNullWhen(true)] out object? result)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (entity is { } && provider.TryFind(entity, out IGraphConverter? converter))
            {
                result = converter.ToModel(entity, host);
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Attempts to convert the specified graph to a model of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of model to convert to.</typeparam>
        /// <param name="provider">The graph converter provider used to perform the conversion.</param>
        /// <param name="entity">The graph node to convert.</param>
        /// <param name="host">The graph host context.</param>
        /// <param name="model">
        /// When this method returns, contains the resulting object of type <typeparamref name="T"/> if successful; 
        /// otherwise, <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> if conversion succeeded; otherwise, <see langword="false"/>.</returns>
        public static bool TryConvert<T>(
            this IGraphConverterProvider provider,
            [NotNullWhen(true)] IGraphNode? entity,
            IAsyncLifecycle host,
            [NotNullWhen(true)] out T? model)
            where T : class
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (entity is { } && provider.TryFind(entity, out IGraphConverter? converter))
            {
                model = converter.ToModel(entity, host) as T;
                return model is { };
            }

            model = default;
            return false;
        }
    }
}

