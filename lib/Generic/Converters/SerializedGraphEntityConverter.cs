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
    public abstract class SerializedGraphEntityConverter<TEntity, TModel, TKey, TValue> : IGraphConverter 
        where TEntity : LifecycleGraphNode<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : class
        where TModel : notnull
    {
#if NET8_0_OR_GREATER
        private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TEntity), typeof(TModel)];
#else
        private static readonly ImmutableArray<Type> _supportedTypes = ImmutableArray.Create(typeof(TEntity), typeof(TModel));
#endif
        /// <inheritdoc cref="IGraphConverter.SupportedTypes"/>
        public virtual ImmutableArray<Type> SupportedTypes => _supportedTypes;
        IReadOnlyCollection<Type> IGraphConverter.SupportedTypes => _supportedTypes;

        /// <inheritdoc/>
        public virtual bool CanConvert([NotNullWhen(true)] object? obj)
        {
            return obj is TEntity || obj is TModel;
        }

        /// <inheritdoc/>
        public virtual int CompareTo(IGraphConverter? other)
        {
            return 0;
        }

        IGraphNode IGraphConverter.ToGraph(object obj, IAsyncLifecycle host, IGraphNode? parent) => ToEntity((TModel)obj, host, parent);

        object IGraphConverter.ToModel(IGraphNode node, IAsyncLifecycle host) => ToModel((TEntity)node, host);

        /// <summary>
        /// Converts the specified <typeparamref name="TModel"/> to it's corresponding <typeparamref name="TEntity"/> representation.
        /// </summary>
        /// <param name="model">
        /// The <typeparamref name="TModel"/> to convert to an <typeparamref name="TEntity"/>. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="host">
        /// The asynchronous lifecycle host that manages the entity's lifecycle. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="parent">
        /// The parent graph node to associate with the new entity, 
        /// or <see langword="null"/> if the entity has no parent.
        /// </param>
        /// <returns>The entity representation of the specified model.</returns>
        protected abstract TEntity ToEntity(TModel model, IAsyncLifecycle host, IGraphNode? parent = null);

        /// <summary>
        /// Converts the specified <typeparamref name="TEntity"/> to it's corresponding <typeparamref name="TModel"/> representation.
        /// </summary>
        /// <param name="node">
        /// The <typeparamref name="TEntity"/> to convert to an <typeparamref name="TModel"/>. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="host">
        /// The asynchronous lifecycle host that manages the entity's lifecycle. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <returns>The model representation of the specified entity node.</returns>
        protected abstract TModel ToModel(TEntity node, IAsyncLifecycle host);
    }
}
