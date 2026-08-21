using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GameSharp.Collections;

internal sealed class TypeRegistryProvider
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
        _loadContexts = RuntimeFeature.IsDynamicCodeSupported ? [AssemblyLoadContext.Default] : null!;
        _availableAssemblyIDs = [];
        _syncRoot = new();
    }

    public TypeRegistry GetOrAdd(Assembly assembly)
    {
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
            registry = new TypeRegistry(checked((short)registriesByID.Length));
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

        if (RuntimeFeature.IsDynamicCodeSupported
            && assembly.IsCollectible
            && AssemblyLoadContext.GetLoadContext(assembly) is { } loadContext
            && TryAddLoadContext(loadContext))
        {
            loadContext.Unloading += OnUnloading;
        }

        return registry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(Assembly assembly, [NotNullWhen(true)] out TypeRegistry? registry)
    {
        return _registriesByAssembly.TryGetValue(assembly, out registry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeRegistry Get(short assemblyID)
    {
        return _registriesByID[assemblyID]!;
    }

    private void OnUnloading(AssemblyLoadContext context)
    {
        if (!TryRemoveLoadContext(context)
            || !RemoveAssemblies(context.Assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out ReadOnlySpan<TypeRegistry> removed))
        {
            return;
        }

        int taskCount = remaining.Length + removed.Length;
        Task[] taskArray = ArrayPool<Task>.Shared.Rent(taskCount);
        try
        {
            ref Task task = ref MemoryMarshal.GetArrayDataReference(taskArray);

            foreach (TypeRegistry registry in removed)
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

    private bool RemoveAssemblies(IEnumerable<Assembly> assemblies, out ReadOnlySpan<TypeRegistry?> remaining, out ReadOnlySpan<TypeRegistry> removed)
    {
        List<TypeRegistry> registryList = [];

        lock (_syncRoot)
        {
            ImmutableDictionary<Assembly, TypeRegistry>.Builder byAssemblyBuilder = _registriesByAssembly.ToBuilder();
            TypeRegistry?[] byID = [.. _registriesByID];

            foreach (Assembly assembly in assemblies)
            {
                if (byAssemblyBuilder.Remove(assembly, out TypeRegistry? registry))
                {
                    byID[registry.AssemblyID] = null;
                    registryList.Add(registry);
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
