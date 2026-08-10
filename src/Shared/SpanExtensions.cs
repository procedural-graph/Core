using System;

namespace ProceduralGraph;

internal static class SpanExtensions
{
    public const int BinarySearchThreshold = 16;

    public static int IndexOfSorted<TItem, TComparand>(this ReadOnlySpan<TItem> values, TComparand value) where TItem : IComparable<TComparand>
    {
        int low = 0, length = values.Length;

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