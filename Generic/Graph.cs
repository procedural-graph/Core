using Microsoft.Extensions.DependencyInjection;
using ProceduralGraph.Collections;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProceduralGraph.Generic;

/// <summary>
/// Represents an abstract, generic graph structure that manages the instantiation and destruction of graph entities
/// based on scene member objects. Provides mechanisms for serialization, deserialization, and traversal of graph
/// elements.
/// </summary>
/// <inheritdoc/>
public sealed class Graph<TKey, TValue> : 
    LifecycleGraphNode<TKey, TValue>, 
    IGraphNode,
    IGraph,
    IDictionary<TValue, GraphEntity<TKey, TValue>> 
    where TKey : struct, IEquatable<TKey> 
    where TValue : class
{
    /// <summary>
    /// Gets the collection of graph converters used to serialize and deserialize graph elements.
    /// </summary>
    public IGraphConverterProvider Converters { get; }

    /// <summary>
    /// Gets the logger instance used to record diagnostic and operational messages for the current node.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the provider that supplies information about scene members for the specified key and value types.
    /// </summary>
    protected ISceneMemberInfoProvider<TKey, TValue> SceneMemberInfoProvider { get; }

    private ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>>? _roots;
    ICollection<IGraphNode> IGraphNode.Descendants => (ICollection<IGraphNode>)_roots!.Values;

    /// <inheritdoc/>
    public ICollection<TValue> Keys => _roots!.Keys;

    /// <inheritdoc/>
    public ICollection<GraphEntity<TKey, TValue>> Values => _roots!.Values;

    /// <inheritdoc/>
    public int Count => _roots!.Count;

    private readonly IServiceProvider _serviceProvider;

    bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.IsReadOnly => false;

    /// <summary>
    /// Gets the graph entity associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to locate the graph entity. Cannot be <see langword="null"/>.</param>
    /// <returns>The graph entity corresponding to the specified key.</returns>
    public GraphEntity<TKey, TValue> this[TValue key] => _roots![key];
    GraphEntity<TKey, TValue> IDictionary<TValue, GraphEntity<TKey, TValue>>.this[TValue key]
    {
        get => _roots![key];
        set => _roots![key] = value;
    }

    /// <summary>
    /// Initializes a new instance of the Graph class using the specified graph converter provider and service provider.
    /// </summary>
    /// <param name="converters">
    /// The provider of graph converters used to handle conversion of graph data. 
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="serviceProvider">
    /// The service provider used to resolve dependencies required by the Graph instance.
    /// Cannot be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="converters"/> or <paramref name="serviceProvider"/> is <see langword="null"/>.
    /// </exception>
    public Graph(IGraphConverterProvider converters, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Converters = converters ?? throw new ArgumentNullException(nameof(converters));
        SceneMemberInfoProvider = serviceProvider.GetRequiredService<ISceneMemberInfoProvider<TKey, TValue>>();
        Logger = serviceProvider.GetRequiredService<ILogger>();
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        ValueTask stopTask = StopAsync(CancellationToken.None);
        stopTask.Forget(Logger, this, CancellationToken.None);
    }

    /// <inheritdoc/>
    protected override CancellationTokenSource BuildCancellationTokenSource(CancellationToken stoppingToken)
    {
        _roots = new ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>>();
        return base.BuildCancellationTokenSource(stoppingToken);
    }

    /// <summary>
    /// Attempts to add the specified scene member to the graph.
    /// </summary>
    /// <param name="sceneMember">The scene member to add to the graph. Must not be null.</param>
    /// <param name="entity">
    /// When this method returns, contains the graph entity associated with the added scene member if the operation
    /// succeeded; otherwise, <see langword="null"/>. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the scene member was successfully added to the graph or already exists; otherwise, <see langword="false"/>.</returns>
    public bool Add(TValue sceneMember, out GraphEntity<TKey, TValue>? entity)
    {
        TValue sceneMemberRoot = SceneMemberInfoProvider.GetRoot(sceneMember);
        if (!TryGetOrAddRoot(sceneMemberRoot, out GraphEntity<TKey, TValue>? rootEntity))
        {
            entity = null;
            return false;
        }

        if (sceneMemberRoot == sceneMember)
        {
            entity = rootEntity;
            return true;
        }

        if (Converters.TryFind(sceneMember, out IGraphConverter? converter))
        {
            entity = InsertAsEntity(sceneMember, rootEntity, converter);
            return entity is { };
        }

        entity = null;
        return false;
    }

    private bool TryGetOrAddRoot(TValue sceneRoot, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? entity)
    {
        if (!Converters.TryFind(sceneRoot, out IGraphConverter? converter))
        {
            entity = null;
            return false;
        }

        ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>> roots = _roots!;

        do
        {
            if (roots.TryGetValue(sceneRoot, out entity))
            {
                return true;
            }

            if (entity is { })
            {
                continue;
            }

            entity = converter.ToGraph(sceneRoot, this, null) as GraphEntity<TKey, TValue>;
            if (entity is null)
            {
                return false;
            }
        }
        while (!roots.TryAdd(sceneRoot, entity!));

        entity!.Start(StoppingToken);

        return true;
    }

    private GraphEntity<TKey, TValue>? InsertAsEntity(TValue sceneMember, GraphEntity<TKey, TValue> rootEntity, IGraphConverter converter)
    {
        CancellationToken stoppingToken = rootEntity.StoppingToken;
        TValue parentSceneMember = SceneMemberInfoProvider.GetParent(sceneMember)!;
        using BreadthFirstTraversalEnumerator<TKey, TValue> enumerator = new(rootEntity);
        while (enumerator.MoveNext())
        {
            stoppingToken.ThrowIfCancellationRequested();

            GraphEntity<TKey, TValue> current = enumerator.Current;
            TKey currentSceneMemberID = GraphEntity<TKey, TValue>.SceneMemberIdentity(current);

            if (!SceneMemberInfoProvider.Equals(currentSceneMemberID, parentSceneMember))
            {
                continue;
            }

            if (converter.ToGraph(sceneMember, this, current) is GraphEntity<TKey, TValue> entity)
            {
                current.Children.Add(entity);
                entity.Start(StoppingToken);
                return entity;
            }

            break;
        }

        return null;
    }

    /// <inheritdoc/>
    public bool Remove(TValue item)
    {
        ConcurrentDictionary<TValue, GraphEntity<TKey, TValue>> roots = _roots!;

        TValue sceneRoot = SceneMemberInfoProvider.GetRoot(item);
        if (!roots.TryGetValue(sceneRoot, out GraphEntity<TKey, TValue>? rootEntity))
        {
            return false;
        }

        if (!TryFindParent(sceneRoot, rootEntity, out GraphEntity<TKey, TValue>? parentEntity))
        {
            return false;
        }

        if (parentEntity == rootEntity)
        {
            StopAndDispose(rootEntity);
            return true;
        }

        TKey sceneMemberID = SceneMemberInfoProvider.GetKey(item);

        if (parentEntity.Children.Remove(sceneMemberID, out ImmutableHashSet<GraphEntity<TKey, TValue>>? items))
        {
            using ImmutableHashSet<GraphEntity<TKey, TValue>>.Enumerator enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
            {
                StopAndDispose(enumerator.Current);
            }
        }

        return false;
    }

    private void StopAndDispose(GraphEntity<TKey, TValue> entity)
    {
        ValueTask stopAndDispose = StopAndDisposeAsync(entity, CancellationToken.None);
        stopAndDispose.Forget(Logger, entity, CancellationToken.None);
    }

    private bool TryFindParent(TValue sceneMember, GraphEntity<TKey, TValue> rootEntity, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? parentEntity)
    {
        TValue? parent = SceneMemberInfoProvider.GetParent(sceneMember);
        TKey parentKey = parent is { } ? SceneMemberInfoProvider.GetKey(parent) : default;

        using BreadthFirstTraversalEnumerator<TKey, TValue> enumerator = new(rootEntity);
        while (enumerator.MoveNext())
        {
            GraphEntity<TKey, TValue> current = enumerator.Current;

            if (current is IProxyGraphNode<TValue> proxyNode)
            {
                TValue? currentParent = SceneMemberInfoProvider.GetParent(proxyNode.SceneMember);
                if (SceneMemberInfoProvider.Equals(currentParent, parent))
                {
                    parentEntity = current;
                    return true;
                }
            }

            if (current.Children.TryGetValue(parentKey, out var results))
            {
                parentEntity = results.FirstOrDefault();
                return parentEntity is { };
            }
        }

        parentEntity = null;
        return false;
    }

    private static async ValueTask StopAndDisposeAsync(GraphEntity<TKey, TValue> entity, CancellationToken cancellationToken)
    {
        try
        {
            ValueTask stop = entity.StopAsync(cancellationToken);
            await stop.ConfigureAwait(false);
        }
        finally
        {
            entity.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(TValue key)
    {
        return _roots!.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TValue key, [NotNullWhen(true)] out GraphEntity<TKey, TValue>? value)
    {
        return _roots!.TryGetValue(key, out value);
    }

    /// <summary>
    /// Constructs a hierarchy of graph entities starting from the specified root entity and using the provided child models.
    /// </summary>
    /// <param name="root">
    /// The root graph entity from which to begin loading the graph structure. This parameter must not be 
    /// <see langword="null"/>.
    /// </param>
    /// <param name="children">
    /// A <see cref="ReadOnlySpan{T}"/> containing the child models that represent components of the graph. Each model must 
    /// specify a valid parent identifier.
    /// </param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> containing the scene members that were entitized and loaded from the provided models. 
    /// The set will be empty if no scene members are found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a converter cannot be found for a model, if a model's parent cannot be resolved, or if a required
    /// scene member cannot be found.
    /// </exception>
    public HashSet<TValue> ConstructHierarchy(GraphEntity<TKey, TValue> root, ReadOnlySpan<GraphComponent<TKey, TValue>.Model> children)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(root);
#else
        if (root is null)
        {
            throw new ArgumentNullException(nameof(root));
        }
#endif

        IGraphConverterProvider converters = Converters;
        ISceneMemberInfoProvider<TKey, TValue> sceneMemberInfoProvider = SceneMemberInfoProvider;

        HashSet<TValue> createdSceneMembers = [];
        Dictionary<Guid, IGraphNode> nodes = new(8)
        {
            { root.ID, root }
        };

        for (int i = 0; i < children.Length; i++)
        {
            GraphComponent<TKey, TValue>.Model model = children[i];

            if (!converters.TryFind(model, out IGraphConverter? converter))
            {
                throw new InvalidOperationException($"No converter found for {model}.");
            }

            if (!nodes.TryGetValue(model.ParentID, out IGraphNode? parent))
            {
                throw new InvalidOperationException($"Unable to resolve parent for {model}.");
            }

            IGraphNode? node;
            if (model is not GraphEntity<TKey, TValue>.Model entityModel || !entityModel.TryGetSceneMemberIdentity(out TKey sceneMemberID))
            {
                node = converter.ToGraph(model, this, parent);
            }
            else
            {
                if (sceneMemberInfoProvider.TryFind(sceneMemberID, out TValue? sceneMember))
                {
                    createdSceneMembers.Add(sceneMember);
                    node = converter.ToGraph(sceneMember, this, entityModel, parent);
                }
                else
                {
                    throw new InvalidOperationException($"Unable to find scene member for {entityModel}.");
                }
            }

            parent.Descendants.Add(node);

            if (node is GraphEntity<TKey, TValue> entity)
            {
                nodes.Add(entity.ID, entity);
            }
        }

        return createdSceneMembers;
    }

    /// <summary>
    /// Constructs a hierarchy of graph nodes starting from the specified root node and adds associated scene members to
    /// the provided collection.
    /// </summary>
    /// <typeparam name="TEntity">
    /// Specifies the type of the root entity, which must implement both the <see cref="IGraphNode"/> and
    /// <see cref="IProxyGraphNode{T}"/> interfaces.
    /// </typeparam>
    /// <param name="root">
    /// The root entity from which the hierarchy construction begins. 
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="createdSceneMembers">
    /// A collection used to track scene members that have already been created, preventing duplicates during hierarchy
    /// construction. Must not be <see langword="null"/>.
    /// .</param>
    public void ConstructHierarchy<TEntity>(TEntity root, HashSet<TValue> createdSceneMembers) where TEntity : IGraphNode, IProxyGraphNode<TValue>
    {
        ISceneMemberInfoProvider<TKey, TValue> sceneMemberInfoProvider = SceneMemberInfoProvider;
        IGraphConverterProvider converterProvider = Converters;
        Stack<KeyValuePair<IGraphNode, TValue>> stack = [];
        KeyValuePair<IGraphNode, TValue> current = new(root, root.SceneMember);
        do
        {
            if (createdSceneMembers.Add(current.Value))
            {
                if (!converterProvider.TryFind(current.Value, out IGraphConverter? converter))
                {
                    continue;
                }

                IGraphNode? graphNode = converter.ToGraph(current.Value, this, current.Key);
                current.Key.Descendants.Add(graphNode);
            }

            IReadOnlyCollection<TValue> collection = sceneMemberInfoProvider.GetChildren(current.Value);
            switch (collection)
            {
                case TValue[] array: PushChildren(current.Key, stack, array); break;
                case List<TValue> list: PushChildren(current.Key, stack, list); break;
                default: PushChildren(current.Key, stack, collection); break;
            }
        }
        while (stack.TryPop(out current));
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TValue>> stack, TValue[] array)
    {
        int length = array.Length;
        if (length == 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        stack.EnsureCapacity(stack.Count + length);
#endif
        for (int i = 0; i < length; i++)
        {
            KeyValuePair<IGraphNode, TValue> item = new(node, array[i]);
            stack.Push(item);
        }
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TValue>> stack, List<TValue> list)
    {
        int count = list.Count;
        if (count == 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        stack.EnsureCapacity(stack.Count + count);
#endif
        using List<TValue>.Enumerator enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<IGraphNode, TValue> item = new(node, enumerator.Current);
            stack.Push(item);
        }
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TValue>> stack, IReadOnlyCollection<TValue> collection)
    {
        int count = collection.Count;
        if (count == 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        stack.EnsureCapacity(stack.Count + count);
#endif
        foreach (TValue item in collection)
        {
            KeyValuePair<IGraphNode, TValue> pair = new(node, item);
            stack.Push(pair);
        }
    }

    /// <summary>
    /// Collapses the hierarchy of the specified graph entity into a flat list of models.
    /// </summary>
    /// <param name="entity">The graph entity to collapse. This parameter cannot be null.</param>
    /// <returns>A list of objects representing the collapsed models derived from the hierarchy of the specified graph entity.</returns>
    public List<object> CollapseHierarchy(GraphEntity<TKey, TValue> entity)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(entity);
#else
        if (entity is null)
        {
            throw new ArgumentNullException(nameof(entity));
        }
#endif

        List<object> models = [];

        using DepthFirstGraphTraverser<TKey, TValue> traverser = new(entity);
        while (traverser.MoveNext())
        {
            GraphEntity<TKey, TValue> current = traverser.Current;

            if (current is GenerativeGraphEntity<TKey, TValue> generativeGraphEntity)
            {
                CollapseComponents(generativeGraphEntity, models);
            }

            CollapseChildren(current, models);
        }

        return models;
    }

    private void CollapseComponents(GenerativeGraphEntity<TKey, TValue> generativeGraphEntity, List<object> models)
    {
        ConcurrentList<GraphComponent<TKey, TValue>> components = generativeGraphEntity.Components;
        int componentCount = components.Count;

        if (componentCount == 0)
        {
            return;
        }

#if NET6_0_OR_GREATER
        models.EnsureCapacity(models.Count + componentCount);
#endif

        IGraphConverterProvider converters = Converters;
        using ImmutableList<GraphComponent<TKey, TValue>>.Enumerator enumerator = components.GetEnumerator();
        while (enumerator.MoveNext())
        {
            ConvertAndAdd(models, converters, enumerator.Current, this);
        }
    }

    private void CollapseChildren(GraphEntity<TKey, TValue> entity, List<object> models)
    {
        ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>> children = entity.Children;
        int childCount = children.Count;

        if (childCount == 0)
        {
            return;
        }

#if NET6_0_OR_GREATER
        models.EnsureCapacity(models.Count + childCount);
#endif

        IGraphConverterProvider converters = Converters;
        using ConcurrentGroupedCollection<TKey, GraphEntity<TKey, TValue>>.Enumerator enumerator = children.GetEnumerator();
        while (enumerator.MoveNext())
        {
            ConvertAndAdd(models, converters, enumerator.Current, this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConvertAndAdd<T>(List<object> models, IGraphConverterProvider converters, T node, Graph<TKey, TValue> graph) where T : IGraphNode
    {
        if (converters.TryFind(node, out IGraphConverter? converter))
        {
            object model = converter.ToModel(node, graph);
            models.Add(model);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TValue, GraphEntity<TKey, TValue>>> GetEnumerator()
    {
        return _roots!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _roots!.GetEnumerator();
    }

    void IDictionary<TValue, GraphEntity<TKey, TValue>>.Add(TValue key, GraphEntity<TKey, TValue> value)
    {
        if (_roots!.TryAdd(key, value))
        {
            value.Start(StoppingToken);
        }
    }

    void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Add(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
    {
        ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Add(item);
        item.Value.Start(StoppingToken);
    }

    void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Clear()
    {
        throw new NotSupportedException("Clearing the graph is not supported. Remove individual items instead.");
    }

    bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Contains(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
    {
        return ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Contains(item);
    }

    void ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.CopyTo(KeyValuePair<TValue, GraphEntity<TKey, TValue>>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>.Remove(KeyValuePair<TValue, GraphEntity<TKey, TValue>> item)
    {
        if (((ICollection<KeyValuePair<TValue, GraphEntity<TKey, TValue>>>)_roots!).Remove(item))
        {
            StopAndDispose(item.Value);
            return true;
        }

        return false;
    }
}
