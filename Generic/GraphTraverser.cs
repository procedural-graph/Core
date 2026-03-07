using ProceduralGraph.Collections;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Generic;

internal static class GraphTraverser
{
    public static bool TryGetNonZeroChildren<TSceneMember>(
        GraphEntity<TSceneMember> entity,
        out ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> children,
        out int childCount)
        where TSceneMember : class
    {
        children = entity.Children;
        childCount = children.Count;
        return childCount > 0;
    }

    public static void Grow<T>(int requiredCapacity, ref T[] rentedArray, int count, int startIndex = 0)
    {
        ArrayPool<T> sharedPool = ArrayPool<T>.Shared;
        T[] newArray = sharedPool.Rent(Math.Max(requiredCapacity, rentedArray.Length * 2));
        try
        {
            if (count > 0)
            {
                Array.Copy(rentedArray, startIndex, newArray, 0, count);
            }
            sharedPool.Return(rentedArray, clearArray: true);
            rentedArray = newArray;
        }
        catch 
        {
            sharedPool.Return(newArray, clearArray: true);
            throw;
        }
    }

    public static int AddSortedChildren<TSceneMember>(
        GraphEntity<TSceneMember>[] array,
        ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> items,
        int arrayIndex)
        where TSceneMember : class
    {
        int copiedCount = items.CopyTo(array, arrayIndex);
        Array.Sort(array, arrayIndex, copiedCount, GraphEntity<TSceneMember>.comparer);
        return copiedCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AddChildren<TSceneMember>(
        GraphEntity<TSceneMember>[] array,
        ConcurrentGroupedCollection<TSceneMember, GraphEntity<TSceneMember>> items,
        int arrayIndex)
        where TSceneMember : class
    {
        return items.CopyTo(array, arrayIndex);
    }

    public static T Pop<T>(T[] array, int index)
    {
        ref T next = ref array[index];
        (T result, next) = (next, default!);
        return result;
    }

    public static T[] RentDefaultAllocationSize<T>()
    {
        return ArrayPool<T>.Shared.Rent(16);
    }

    public static bool Return<T>([NotNullWhen(true)] T[]? array) where T : class
    {
        if (array is { })
        {
            ArrayPool<T>.Shared.Return(array, clearArray: true);
            return true;
        }

        return false;
    }
}
