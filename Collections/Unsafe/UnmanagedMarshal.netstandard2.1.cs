using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Collections.Unsafe
{
    public static partial class UnmanagedMarshal
    {
        internal static unsafe T* AllocZeroed<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be non-negative.");
            }

            int size = sizeof(T) * elementCount;
            T* ptr = (T*)Marshal.AllocHGlobal(size);
            Buffer.MemoryCopy(null, ptr, size, 0);
            return ptr;
        }

        internal static unsafe T* Alloc<T>(int elementCount) where T : unmanaged
        {
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be non-negative.");
            }
            int size = sizeof(T) * elementCount;
            return (T*)Marshal.AllocHGlobal(size);
        }

        internal static unsafe void Free<T>(T* buffer) where T : unmanaged
        {
            Marshal.FreeHGlobal((IntPtr)buffer);
        }

        internal static unsafe void Clear<T>(T* buffer, int elementCount) where T : unmanaged
        {
            int size = sizeof(T) * elementCount;
            Buffer.MemoryCopy(null, buffer, size, 0);
        }

        internal static unsafe void Copy<T>(T* source, T* destination, int elementCount) where T : unmanaged
        {
            int size = sizeof(T) * elementCount;
            Buffer.MemoryCopy(source, destination, size, size);
        }
    }
}
