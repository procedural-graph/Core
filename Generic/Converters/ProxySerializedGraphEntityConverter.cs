using System;
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
    public abstract class ProxySerializedGraphEntityConverter<TEntity, TModel, TSceneMember, TKey, TValue> : GraphNodeSerializer<TEntity, TModel>, IGraphConverter
        where TEntity : LifecycleGraphNode<TKey, TValue>, IProxyGraphNode<TValue>
        where TSceneMember : class, TValue
        where TKey : struct, IEquatable<TKey>
        where TValue : class
        where TModel : class
    {
        private const string ProxySceneMemberRequiredMessageFormat = 
            "Proxy entities must be associated with a scene member to be converted to a graph node. " +
            "Use {0} with the model parameter instead.";

#if NET8_0_OR_GREATER
        private static readonly ImmutableArray<Type> _supportedTypes = [typeof(TSceneMember), typeof(TEntity), typeof(TModel)];
#else
        private static readonly ImmutableArray<Type> _supportedTypes = ImmutableArray.Create(typeof(TSceneMember), typeof(TEntity), typeof(TModel));
#endif
        /// <inheritdoc/>
        public override ImmutableArray<Type> SupportedTypes => _supportedTypes;

        /// <inheritdoc/>
        public override bool CanConvert([NotNullWhen(true)] object? obj)
        {
            return base.CanConvert(obj) || obj is TSceneMember;
        }

        /// <summary>
        /// Converts the specified <typeparamref name="TSceneMember"/> to it's corresponding <typeparamref name="TEntity"/> representation.
        /// </summary>
        /// <param name="sceneMember">
        /// The <typeparamref name="TSceneMember"/> to convert to an <typeparamref name="TEntity"/>. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="root">
        /// The asynchronous lifecycle host that manages the entity's lifecycle. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="model">The model representation to use for the conversion, or <see langword="null"/> if no model is available.</param>
        /// <param name="parent">
        /// The parent graph node to associate with the new entity, 
        /// or <see langword="null"/> if the entity has no parent.
        /// </param>
        /// <returns>The entity representation of the specified scene member.</returns>
        protected abstract TEntity ToEntity(TSceneMember sceneMember, IGraph root, TModel? model, IGraphNode? parent = null);

        IGraphNode IGraphConverter.ToGraph(object obj, IGraph root, IGraphNode? parent)
        {
            try
            {
                TSceneMember typedSceneMember = obj as TSceneMember ?? throw new ArgumentException($"Must be of type {typeof(TSceneMember)}.", nameof(obj));
                return ToEntity(typedSceneMember, root, null, parent);
            }
            catch (ArgumentException ex) when (obj is TModel)
            {
                string message = string.Format(ProxySceneMemberRequiredMessageFormat, nameof(IGraphConverter.ToGraph));
                throw new NotSupportedException(ProxySceneMemberRequiredMessageFormat, ex);
            }
        }

        IGraphNode IGraphConverter.ToGraph(object sceneMember, IGraph root, object model, IGraphNode? parent)
        {
            TSceneMember typedSceneMember = sceneMember as TSceneMember ?? throw new ArgumentException($"Must be of type {typeof(TSceneMember)}", nameof(sceneMember));
            TModel typedModel = model as TModel ?? throw new ArgumentException($"Must be of type {typeof(TModel)}", nameof(model));
            return ToEntity(typedSceneMember, root, typedModel, parent);
        }
    }
}
