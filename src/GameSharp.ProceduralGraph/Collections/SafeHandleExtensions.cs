using ProceduralGraph;
using System.Collections.Generic;

namespace GameSharp.ProceduralGraph.Collections;

internal static partial class SafeHandleExtensions
{
    [Guard]
    public static partial bool Contains<T>(this SafeHandle handle, T item, ulong length) where T : unmanaged;

    private static unsafe bool ContainsImpl<T>(SafeHandle.LeasedHandle leasedHandle, T item, ulong length) where T : unmanaged
    {
        T* pos = (T*)leasedHandle.Handle;
        T* end = pos + length;

#if NETCOREAPP3_0_OR_GREATER
        if (System.Numerics.Vector.IsHardwareAccelerated)
        {
            int count = System.Numerics.Vector<T>.Count;
            for (; pos < end; pos += count)
            {
                System.Numerics.Vector<T> vector = *(System.Numerics.Vector<T>*)pos;
                if (System.Numerics.Vector.Any(vector, item))
                {
                    return true;
                }
            }
        }

#endif
        EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
        for (; pos < end; pos++)
        {
            if (equalityComparer.Equals(*pos, item))
            {
                return true;
            }
        }

        return false;
    }
}
