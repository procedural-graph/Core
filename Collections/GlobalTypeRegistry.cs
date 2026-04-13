using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProceduralGraph.Collections;

internal class GlobalTypeRegistry
{
    private static readonly ConcurrentDictionary<Type, TypeRegistration> _types = new();
    private static int _currentTypeID;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeRegistration Get<T>()
    {
        return Get(typeof(T));
    }

    public static TypeRegistration Get(Type type)
    {
        if (_types.TryGetValue(type, out TypeRegistration currentReg))
        {
            return currentReg;
        }

        TypeRegistration newReg = new(Interlocked.Increment(ref _currentTypeID));
        currentReg = _types.GetOrAdd(type, newReg);

        if (currentReg == newReg)
        {
            Type? baseType = type.BaseType;
            while (baseType is { })
            {
                _types.AddOrUpdate(baseType, Add, Update, newReg.ID);
                baseType = baseType.BaseType;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                _types.AddOrUpdate(interfaceType, Add, Update, newReg.ID);
            }
        }

        return currentReg;
    }

    private static TypeRegistration Add(Type key, int inheritorID)
    {
        int id = Interlocked.Increment(ref _currentTypeID);
        int[] array = [id, inheritorID];
        Array.Sort(array);
        return new TypeRegistration()
        {
            ID = id,
            Order = array[0] == id ? 0 : 1,
            DerivedTypes = ImmutableCollectionsMarshal.AsImmutableArray(array)
        };
    }

    private static TypeRegistration Update(Type key, TypeRegistration existing, int inheritorID)
    {
        ImmutableArray<int> derived = existing.DerivedTypes;

        ReadOnlySpan<int> derivedSpan = derived.AsSpan();
        int index = derivedSpan.IndexOfSorted(inheritorID);

        return index >= 0 ? existing : existing with
        {
            DerivedTypes = derived.Insert(~index, inheritorID),
            Order = inheritorID < existing.ID ? existing.Order + 1 : existing.Order
        };
    }
}
