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
public sealed class Graph<TSceneMember> : 
    LifecycleGraphNode<TSceneMember>, 
    IGraphNode,
    IGraph,
    IDictionary<TSceneMember, GraphEntity<TSceneMember>> 
    where TSceneMember : class
{
    /// <summary>
    /// Gets the collection of graph converters used to serialize and deserialize graph elements.
    /// </summary>
    public IGraphConverterProvider Converters { get; }

    /// <summary>
    /// Gets the logger instance used to record diagnostic and operational messages for the current node.
    /// </summary>
    public ILogger Logger { get; }

    private readonly ISceneMemberInfoProvider<TSceneMember> _sceneMemberInfoProvider;

    private ConcurrentDictionary<TSceneMember, GraphEntity<TSceneMember>>? _roots;
    ICollection<IGraphNode> IGraphNode.Descendants => (ICollection<IGraphNode>)_roots!.Values;

    /// <inheritdoc/>
    public ICollection<TSceneMember> Keys => _roots!.Keys;

    /// <inheritdoc/>
    public ICollection<GraphEntity<TSceneMember>> Values => _roots!.Values;

    /// <inheritdoc/>
    public int Count => _roots!.Count;

    private readonly IServiceProvider _serviceProvider;

    bool ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.IsReadOnly => false;

    /// <summary>
    /// Gets the graph entity associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to locate the graph entity. Cannot be <see langword="null"/>.</param>
    /// <returns>The graph entity corresponding to the specified key.</returns>
    public GraphEntity<TSceneMember> this[TSceneMember key] => _roots![key];
    GraphEntity<TSceneMember> IDictionary<TSceneMember, GraphEntity<TSceneMember>>.this[TSceneMember key]
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
        _sceneMemberInfoProvider = serviceProvider.GetRequiredService<ISceneMemberInfoProvider<TSceneMember>>();
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
        _roots = new ConcurrentDictionary<TSceneMember, GraphEntity<TSceneMember>>();
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
    public bool Add(TSceneMember sceneMember, out GraphEntity<TSceneMember>? entity)
    {
        TSceneMember sceneMemberRoot = _sceneMemberInfoProvider.GetRoot(sceneMember);
        if (!TryGetOrAddRoot(sceneMemberRoot, out GraphEntity<TSceneMember>? rootEntity))
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

    private bool TryGetOrAddRoot(TSceneMember sceneRoot, [NotNullWhen(true)] out GraphEntity<TSceneMember>? entity)
    {
        if (!Converters.TryFind(sceneRoot, out IGraphConverter? converter))
        {
            entity = null;
            return false;
        }

        ConcurrentDictionary<TSceneMember, GraphEntity<TSceneMember>> roots = _roots!;

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

            entity = converter.ToGraph(sceneRoot, this, null) as GraphEntity<TSceneMember>;
            if (entity is null)
            {
                return false;
            }
        }
        while (!roots.TryAdd(sceneRoot, entity!));

        entity!.Start(StoppingToken);

        return true;
    }

    private GraphEntity<TSceneMember>? InsertAsEntity(TSceneMember sceneMember, GraphEntity<TSceneMember> rootEntity, IGraphConverter converter)
    {
        CancellationToken stoppingToken = rootEntity.StoppingToken;
        TSceneMember parentSceneMember = _sceneMemberInfoProvider.GetParent(sceneMember)!;
        using BreadthFirstTraversalEnumerator<TSceneMember> enumerator = new(rootEntity);
        while (enumerator.MoveNext())
        {
            stoppingToken.ThrowIfCancellationRequested();

            GraphEntity<TSceneMember> current = enumerator.Current;
            TSceneMember? currentSceneMember = GraphEntity<TSceneMember>.GetSceneMember(current);

            if (!_sceneMemberInfoProvider.Equals(currentSceneMember, parentSceneMember))
            {
                continue;
            }

            if (converter.ToGraph(sceneMember, this, current) is GraphEntity<TSceneMember> entity)
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
    public bool Remove(TSceneMember item)
    {
        ConcurrentDictionary<TSceneMember, GraphEntity<TSceneMember>> roots = _roots!;

        TSceneMember sceneRoot = _sceneMemberInfoProvider.GetRoot(item);
        if (!roots.TryGetValue(sceneRoot, out GraphEntity<TSceneMember>? rootEntity))
        {
            return false;
        }

        if (!TryFindParent(sceneRoot, rootEntity, out GraphEntity<TSceneMember>? parentEntity))
        {
            return false;
        }

        if (parentEntity == rootEntity)
        {
            StopAndDispose(rootEntity);
            return true;
        }

        if (parentEntity.Children.Remove(item, out ImmutableHashSet<GraphEntity<TSceneMember>>? items))
        {
            foreach (GraphEntity<TSceneMember> entity in items)
            {
                StopAndDispose(entity);
            }
        }

        return false;
    }

    private void StopAndDispose(GraphEntity<TSceneMember> entity)
    {
        ValueTask stopAndDispose = StopAndDisposeAsync(entity, CancellationToken.None);
        stopAndDispose.Forget(Logger, entity, CancellationToken.None);
    }

    private bool TryFindParent(TSceneMember sceneMember, GraphEntity<TSceneMember> rootEntity, [NotNullWhen(true)] out GraphEntity<TSceneMember>? parentEntity)
    {
        TSceneMember? parent = _sceneMemberInfoProvider.GetParent(sceneMember);
        using BreadthFirstTraversalEnumerator<TSceneMember> enumerator = new(rootEntity);
        while (enumerator.MoveNext())
        {
            GraphEntity<TSceneMember> current = enumerator.Current;

            if (current is IProxyGraphNode<TSceneMember> proxyNode)
            {
                TSceneMember? currentParent = _sceneMemberInfoProvider.GetParent(proxyNode.SceneMember);
                if (_sceneMemberInfoProvider.Equals(currentParent, parent))
                {
                    parentEntity = current;
                    return true;
                }
            }

            if (current.Children.TryGetValue(parent, out var results))
            {
                parentEntity = results.FirstOrDefault();
                return parentEntity is { };
            }
        }

        parentEntity = null;
        return false;
    }

    private static async ValueTask StopAndDisposeAsync(GraphEntity<TSceneMember> entity, CancellationToken cancellationToken)
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
    public bool ContainsKey(TSceneMember key)
    {
        return _roots!.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TSceneMember key, [NotNullWhen(true)] out GraphEntity<TSceneMember>? value)
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
    public HashSet<TSceneMember> ConstructHierarchy(GraphEntity<TSceneMember> root, ReadOnlySpan<GraphComponent<TSceneMember>.Model> children)
    {
        ThrowHelpers.ThrowIfNull(root);

        IGraphConverterProvider converters = Converters;
        ISceneMemberInfoProvider<TSceneMember> sceneMemberInfoProvider = _sceneMemberInfoProvider;

        HashSet<TSceneMember> createdSceneMembers = [];
        Dictionary<Guid, IGraphNode> nodes = new(8)
        {
            { root.ID, root }
        };

        for (int i = 0; i < children.Length; i++)
        {
            GraphComponent<TSceneMember>.Model model = children[i];

            if (!converters.TryFind(model, out IGraphConverter? converter))
            {
                throw new InvalidOperationException($"No converter found for {model}.");
            }

            if (!nodes.TryGetValue(model.ParentID, out IGraphNode? parent))
            {
                throw new InvalidOperationException($"Unable to resolve parent for {model}.");
            }

            IGraphNode? node;
            if (model is GraphEntity<TSceneMember>.Model entityModel && entityModel.TryGetSceneMember(out TSceneMember? sceneMember))
            {
                createdSceneMembers.Add(sceneMember);
                node = converter.ToGraph(sceneMember, entityModel, parent);
            }
            else
            {
                node = converter.ToGraph(model, this, parent);
            }

            parent.Descendants.Add(node);

            if (node is GraphEntity<TSceneMember> entity)
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
    public void ConstructHierarchy<TEntity>(TEntity root, HashSet<TSceneMember> createdSceneMembers) where TEntity : IGraphNode, IProxyGraphNode<TSceneMember>
    {
        ISceneMemberInfoProvider<TSceneMember> sceneMemberInfoProvider = _sceneMemberInfoProvider;
        IGraphConverterProvider converterProvider = Converters;
        Stack<KeyValuePair<IGraphNode, TSceneMember>> stack = [];
        KeyValuePair<IGraphNode, TSceneMember> current = new(root, root.SceneMember);
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

            IReadOnlyCollection<TSceneMember> collection = sceneMemberInfoProvider.GetChildren(current.Value);
            switch (collection)
            {
                case TSceneMember[] array: PushChildren(current.Key, stack, array); break;
                case List<TSceneMember> list: PushChildren(current.Key, stack, list); break;
                default: PushChildren(current.Key, stack, collection); break;
            }
        }
        while (stack.TryPop(out current));
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TSceneMember>> stack, TSceneMember[] array)
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
            KeyValuePair<IGraphNode, TSceneMember> item = new(node, array[i]);
            stack.Push(item);
        }
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TSceneMember>> stack, List<TSceneMember> list)
    {
        int count = list.Count;
        if (count == 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        stack.EnsureCapacity(stack.Count + count);
#endif
        using List<TSceneMember>.Enumerator enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
        {
            KeyValuePair<IGraphNode, TSceneMember> item = new(node, enumerator.Current);
            stack.Push(item);
        }
    }

    private static void PushChildren(IGraphNode node, Stack<KeyValuePair<IGraphNode, TSceneMember>> stack, IReadOnlyCollection<TSceneMember> collection)
    {
        int count = collection.Count;
        if (count == 0)
        {
            return;
        }
#if NET6_0_OR_GREATER
        stack.EnsureCapacity(stack.Count + count);
#endif
        foreach (TSceneMember item in collection)
        {
            KeyValuePair<IGraphNode, TSceneMember> pair = new(node, item);
            stack.Push(pair);
        }
    }

    /// <summary>
    /// Collapses the hierarchy of the specified graph entity into a flat list of models.
    /// </summary>
    /// <param name="entity">The graph entity to collapse. This parameter cannot be null.</param>
    /// <returns>A list of objects representing the collapsed models derived from the hierarchy of the specified graph entity.</returns>
    public List<object> CollapseHierarchy(GraphEntity<TSceneMember> entity)
    {
        ThrowHelpers.ThrowIfNull(entity);

        List<object> models = [];

        using DepthFirstGraphTraverser<TSceneMember> traverser = new(entity);
        while (traverser.MoveNext())
        {
            GraphEntity<TSceneMember> current = traverser.Current;

            if (current is GenerativeGraphEntity<TSceneMember> generativeGraphEntity)
            {
                CollapseComponents(generativeGraphEntity, models);
            }

            CollapseChildren(current, models);
        }

        return models;
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        base.OnDisposing();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void CollapseComponents(GenerativeGraphEntity<TSceneMember> generativeGraphEntity, List<object> models)
    {
        ConcurrentList<GraphComponent<TSceneMember>> components = generativeGraphEntity.Components;
        int componentCount = components.Count;

        if (componentCount == 0)
        {
            return;
        }

#if NET6_0_OR_GREATER
        models.EnsureCapacity(models.Count + componentCount);
#endif

        IGraphConverterProvider converters = Converters;
        using ImmutableList<GraphComponent<TSceneMember>>.Enumerator enumerator = components.GetEnumerator();
        while (enumerator.MoveNext())
        {
            ConvertAndAdd(models, converters, enumerator.Current, this);
        }
    }

    private void CollapseChildren(GraphEntity<TSceneMember> entity, List<object> models)
    {
        ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> children = entity.Children;
        int childCount = children.Count;

        if (childCount == 0)
        {
            return;
        }

#if NET6_0_OR_GREATER
        models.EnsureCapacity(models.Count + childCount);
#endif

        IGraphConverterProvider converters = Converters;
        using ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>>.Enumerator enumerator = children.GetEnumerator();
        while (enumerator.MoveNext())
        {
            ConvertAndAdd(models, converters, enumerator.Current, this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ConvertAndAdd<T>(List<object> models, IGraphConverterProvider converters, T node, Graph<TSceneMember> graph) where T : IGraphNode
    {
        if (converters.TryFind(node, out IGraphConverter? converter))
        {
            object model = converter.ToModel(node);
            models.Add(model);
        }
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>> GetEnumerator()
    {
        return _roots!.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _roots!.GetEnumerator();
    }

    void IDictionary<TSceneMember, GraphEntity<TSceneMember>>.Add(TSceneMember key, GraphEntity<TSceneMember> value)
    {
        if (_roots!.TryAdd(key, value))
        {
            value.Start(StoppingToken);
        }
    }

    void ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.Add(KeyValuePair<TSceneMember, GraphEntity<TSceneMember>> item)
    {
        ((ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>)_roots!).Add(item);
        item.Value.Start(StoppingToken);
    }

    void ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.Clear()
    {
        throw new NotSupportedException("Clearing the graph is not supported. Remove individual items instead.");
    }

    bool ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.Contains(KeyValuePair<TSceneMember, GraphEntity<TSceneMember>> item)
    {
        return ((ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>)_roots!).Contains(item);
    }

    void ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.CopyTo(KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>)_roots!).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>.Remove(KeyValuePair<TSceneMember, GraphEntity<TSceneMember>> item)
    {
        if (((ICollection<KeyValuePair<TSceneMember, GraphEntity<TSceneMember>>>)_roots!).Remove(item))
        {
            StopAndDispose(item.Value);
            return true;
        }

        return false;
    }
}
