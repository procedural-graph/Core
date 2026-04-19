using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace ProceduralGraph;

internal static class GlobalTypeRegistry
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

        TypeRegistration newReg = new(Interlocked.Increment(ref _currentTypeID), type);
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

    public static Type[] LeaseReverseLookup()
    {
        ICollection<TypeRegistration> registrations = _types.Values;
        int currentCount = registrations.Count, oldCount, max;

        TypeRegistration[]? registrationsArray = RentedArray.Acquire<TypeRegistration>(currentCount);
        Type[]? typesArray = null;

        try
        {
            Span<TypeRegistration> registrationsSpan;

            do
            {
                registrationsSpan = RentedArray.Grow(ref registrationsArray, currentCount);

                max = int.MaxValue;
                int index = 0;

                using IEnumerator<TypeRegistration> enumerator = registrations.GetEnumerator();
                while (index < currentCount && enumerator.MoveNext())
                {
                    TypeRegistration current = enumerator.Current;
                    registrationsArray[index++] = current;
                    max = Math.Max(max, current.ID);
                }

                (currentCount, oldCount) = (registrations.Count, currentCount);
            }
            while (currentCount != oldCount);

            typesArray = RentedArray.Acquire<Type>(max + 1);
            foreach (TypeRegistration registration in registrationsSpan)
            {
                typesArray[registration.ID] = registration.Type;
            }

            return typesArray;
        }
        catch
        {
            RentedArray.TryReturn(ref typesArray);
            throw;
        }
        finally
        {
            RentedArray.Return(ref registrationsArray);
        }
    }

    private static TypeRegistration Add(Type key, int inheritorID)
    {
        int id = Interlocked.Increment(ref _currentTypeID);
        int[] array = [id, inheritorID];
        Array.Sort(array);
        return new TypeRegistration()
        {
            ID = id,
            Type = key,
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
