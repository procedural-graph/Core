using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GameSharp.Collections.Collectible;

internal class TypeRegistryProvider : Collections.TypeRegistryProvider
{
    private volatile ImmutableDictionary<Assembly, TypeRegistry> _registriesByAssembly;
    private volatile TypeRegistry?[] _registriesByID;
    private ImmutableHashSet<AssemblyLoadContext> _loadContexts;
    private readonly Queue<short> _availableAssemblyIDs;
    private readonly Lock _syncRoot;

    public TypeRegistryProvider()
    {
        _registriesByAssembly = [];
        _registriesByID = [];
        _loadContexts = [AssemblyLoadContext.Default];
        _availableAssemblyIDs = [];
        _syncRoot = new();
    }

    public override TypeRegistry GetOrAdd(Type type)
    {
        Assembly assembly = type.Assembly;

        ImmutableDictionary<Assembly, TypeRegistry> currRegistries = _registriesByAssembly;
        if (currRegistries.TryGetValue(assembly, out TypeRegistry? registry))
        {
            return registry;
        }

        lock (_syncRoot)
        {
            (ImmutableDictionary<Assembly, TypeRegistry> prevRegistries, currRegistries) = (currRegistries, _registriesByAssembly);

            if (!ReferenceEquals(prevRegistries, currRegistries) && currRegistries.TryGetValue(assembly, out registry))
            {
                return registry;
            }

            TypeRegistry?[] registriesByID = _registriesByID;
            short id = checked((short)registriesByID.Length);
            registry = assembly.IsCollectible ? new CollectibleTypeRegistry(id) : new NonCollectibleTypeRegistry(id);
            _registriesByAssembly = _registriesByAssembly.Add(assembly, registry);
            if (_availableAssemblyIDs.TryDequeue(out short assemblyID))
            {
                _registriesByID = [.. registriesByID.AsSpan(..assemblyID), registry, .. registriesByID.AsSpan(assemblyID + 1)];
            }
            else
            {
                _registriesByID = [.. registriesByID, registry];
            }
        }

        if (assembly.IsCollectible && AssemblyLoadContext.GetLoadContext(assembly) is { } loadContext && TryAddLoadContext(loadContext))
        {
            loadContext.Unloading += OnUnloading;
        }

        return registry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool TryGet(Type type, [NotNullWhen(true)] out Collections.TypeRegistry? registry)
    {
        Unsafe.SkipInit(out registry);
        return _registriesByAssembly.TryGetValue(type.Assembly, out Unsafe.As<Collections.TypeRegistry?, TypeRegistry?>(ref registry));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override TypeRegistry Get(int id)
    {
        return _registriesByID[((TypeIdentifier)id).AssemblyID]!;
    }

    private void OnUnloading(AssemblyLoadContext context)
    {
        if (!TryRemoveLoadContext(context)
            || !RemoveAssemblies(context.Assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out ReadOnlySpan<CollectibleTypeRegistry> removed))
        {
            return;
        }

        int taskCount = remaining.Length + removed.Length;
        Task[] taskArray = ArrayPool<Task>.Shared.Rent(taskCount);
        try
        {
            ref Task task = ref MemoryMarshal.GetArrayDataReference(taskArray);

            foreach (CollectibleTypeRegistry registry in removed)
            {
                task = Task.Run(registry.Dispose);
                task = ref Unsafe.Add(ref task, 1);
            }

            foreach (TypeRegistry? registry in remaining)
            {
                if (registry is null)
                {
                    taskCount--;
                    continue;
                }

                task = PurgeRegistriesAsync(removed, registry);
                task = ref Unsafe.Add(ref task, 1);
            }

            Task.WaitAll(taskArray.AsSpan(0, taskCount));
        }
        finally
        {
            ArrayPool<Task>.Shared.Return(taskArray, clearArray: true);

            lock (_syncRoot)
            {
                _availableAssemblyIDs.EnsureCapacity(_availableAssemblyIDs.Count + removed.Length);
                foreach (TypeRegistry registry in removed)
                {
                    _availableAssemblyIDs.Enqueue(registry.AssemblyID);
                }
            }
        }
    }

    private static Task PurgeRegistriesAsync(ReadOnlySpan<TypeRegistry> removed, TypeRegistry retainedRegistry)
    {
        using TypeRegistry.PurgeContext purgeContext = retainedRegistry.GetPurgeContext();
        int taskCount = purgeContext.Count * removed.Length;
        Task[] taskArray = ArrayPool<Task>.Shared.Rent(taskCount);
        ref Task task = ref MemoryMarshal.GetArrayDataReference(taskArray);
        foreach (TypeRegistry removedRegistry in removed)
        {
            purgeContext.Purge(ref task, removedRegistry.AssemblyID);
        }
        return WaitThenReturnAsync(taskArray, taskCount);
    }

    private static async Task WaitThenReturnAsync(Task[] tasks, int count)
    {
        Task wait = Task.WhenAll(tasks.AsSpan(0, count));
        await wait.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ArrayPool<Task>.Shared.Return(tasks, clearArray: true);
        wait.GetAwaiter().GetResult();
    }

    private bool RemoveAssemblies(IEnumerable<Assembly> assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out ReadOnlySpan<CollectibleTypeRegistry> removed)
    {
        List<CollectibleTypeRegistry> registryList = [];

        lock (_syncRoot)
        {
            ImmutableDictionary<Assembly, TypeRegistry>.Builder byAssemblyBuilder = _registriesByAssembly.ToBuilder();
            TypeRegistry?[] byID = [.. _registriesByID];

            foreach (Assembly assembly in assemblies)
            {
                if (byAssemblyBuilder.Remove(assembly, out TypeRegistry? registry))
                {
                    byID[registry.AssemblyID] = null;
                    registryList.Add((CollectibleTypeRegistry)registry);
                }
            }

            if (registryList.Count == 0)
            {
                removed = default;
                remaining = _registriesByID.AsSpan();
                return false;
            }

            _registriesByAssembly = byAssemblyBuilder.ToImmutable();
            _registriesByID = byID;
            remaining = _registriesByID.AsSpan();
        }

        removed = CollectionsMarshal.AsSpan(registryList);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAddLoadContext(AssemblyLoadContext loadContext)
    {
        return ImmutableInterlocked.Update(ref _loadContexts, static (set, ctx) => set.Add(ctx), loadContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryRemoveLoadContext(AssemblyLoadContext loadContext)
    {
        return ImmutableInterlocked.Update(ref _loadContexts, static (set, ctx) => set.Remove(ctx), loadContext);
    }
}
