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
    /// Represents a base implementation of an entity converter that provides mechanisms for converting between scene members, 
    /// graph entities, and model representations within a graph structure.
    /// </summary>
    /// <typeparam name="TEntity">
    /// The type of graph entity being converted. Must derive from 
    /// <see cref="LifecycleGraphNode{TKey, TValue}"/> and implement <see cref="IProxyGraphNode{TValue}"/>.
    /// </typeparam>
    /// <typeparam name="TModel">The type of the model representation used for serialization and deserialization. Must be a reference type.</typeparam>
    /// <typeparam name="TSceneMember">
    /// The engine-specific type of scene hierarchy member being converted. 
    /// Must derive from <typeparamref name="TValue"/>.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of the key used to identify scene members. Must be a value type that implements 
    /// <see cref="IEquatable{TKey}"/>.
    /// </typeparam>
    /// <typeparam name="TValue">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
    public abstract class ProxySerializedGraphEntityConverter<TEntity, TModel, TSceneMember, TKey, TValue> : 
        SerializedGraphEntityConverter<TEntity, TModel, TKey, TValue>, IGraphConverter
        where TEntity : LifecycleGraphNode<TKey, TValue>, IProxyGraphNode<TValue>
        where TSceneMember : class, TValue
        where TKey : struct, IEquatable<TKey>
        where TValue : class
        where TModel : notnull
    {
#if NET8_0_OR_GREATER
        private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TSceneMember), typeof(TEntity), typeof(TModel)];
#else
        private static readonly ImmutableArray<Type> _supportedTypes = ImmutableArray.Create(typeof(TSceneMember), typeof(TEntity), typeof(TModel));
#endif
        /// <inheritdoc cref="IGraphConverter.SupportedTypes"/>
        public override ImmutableArray<Type> SupportedTypes => _supportedTypes;
        IReadOnlyCollection<Type> IGraphConverter.SupportedTypes => _supportedTypes;

        /// <inheritdoc/>
        public override bool CanConvert([NotNullWhen(true)] object? obj)
        {
            return base.CanConvert(obj) || obj is TSceneMember;
        }

        IGraphNode IGraphConverter.ToGraph(object obj, IAsyncLifecycle host, IGraphNode? parent) => obj switch
        {
            TModel model => ToEntity(model, host, parent),
            TSceneMember sceneMember => ToEntity(sceneMember, host, parent),
            _ => throw new InvalidOperationException($"Unsupported type: {obj.GetType()}")
        };

        object IGraphConverter.ToModel(IGraphNode node, IAsyncLifecycle host) => ToModel((TEntity)node, host);

        /// <summary>
        /// Converts the specified <typeparamref name="TSceneMember"/> to it's corresponding <typeparamref name="TEntity"/> representation.
        /// </summary>
        /// <param name="sceneMember">
        /// The <typeparamref name="TSceneMember"/> to convert to an <typeparamref name="TEntity"/>. 
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
        /// <returns>The entity representation of the specified scene member.</returns>
        protected abstract TEntity ToEntity(TSceneMember sceneMember, IAsyncLifecycle host, IGraphNode? parent = null);
    }
}
