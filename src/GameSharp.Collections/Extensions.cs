using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

internal static partial class Extensions
{
    private static readonly int _l1ExclSize = ProcessorInfo.Default.LineCacheSizeInBytes - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref TItem HybridSearch<TItem, TComparand>(ref TItem low, ref TItem high, TComparand value, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        int divisor = sizeof(TItem) * 2;

        for (nint byteOffset; (byteOffset = Unsafe.ByteOffset(ref low, ref high)) > _l1ExclSize;)
        {
            ref TItem mid = ref Unsafe.Add(ref low, byteOffset / divisor);
            switch (mid.CompareTo(value))
            {
                case 0: exists = true; return ref mid;
                case < 0: low = ref Unsafe.Add(ref mid, 1); break;
                default: high = ref Unsafe.Subtract(ref mid, 1); break;
            }
        }

        for (; Unsafe.IsAddressLessThanOrEqualTo(ref low, ref high); low = ref Unsafe.Add(ref low, 1))
        {
            switch (low.CompareTo(value))
            {
                case 0: exists = true; return ref low;
                case < 0: continue;
            }

            break;
        }

        exists = false;
        return ref low;
    }

    public static ref TItem HybridSearch<TItem, TComparand>(this ReadOnlySpan<TItem> items, TComparand value, out int byteOffset, out bool exists)
        where TItem : unmanaged, IComparable<TComparand>
    {
        ref TItem data = ref MemoryMarshal.GetReference(items), low = ref data, high = ref Unsafe.Add(ref data, items.Length - 1);
        ref TItem result = ref HybridSearch(ref low, ref high, value, out exists);
        byteOffset = (int)Unsafe.ByteOffset(in data, in result);
        return ref result;
    }
}
