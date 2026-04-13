using System;
using System.Collections.Generic;

namespace ProceduralGraph.Collections;

internal static class CollectionExtensions
{
    public const int BinarySearchThreshold = 16;

    public static int IndexOfSorted<TCollection, TItem>(this TCollection values, TItem value) 
        where TCollection : IReadOnlyList<TItem>
        where TItem : IComparable<TItem>
    {
        int low = 0, length = values.Count;

        if (length == 0)
        {
            return ~0;
        }

        if (length < BinarySearchThreshold)
        {
            int comparison;
            do
            {
                comparison = values[low].CompareTo(value);
                if (comparison == 0)
                {
                    return low;
                }
            }
            while (comparison < 0 && ++low < length);
        }
        else
        {
            int high = length - 1;
            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                switch (values[mid].CompareTo(value))
                {
                    case 0: return mid;
                    case < 0: low = mid + 1; break;
                    default: high = mid - 1; break;
                }
            }
        }

        return ~low;
    }
}
