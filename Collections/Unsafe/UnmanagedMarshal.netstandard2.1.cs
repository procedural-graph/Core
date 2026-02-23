using System;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Collections.Unsafe
{
    public static partial class UnmanagedMarshal
    {
        /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/AllocZeroed/*'/>
        public static unsafe T* AllocZeroed<T>(int elementCount) where T : unmanaged
        {
            void* ptr = Alloc(elementCount, sizeof(T), out long byteCount);
            Buffer.MemoryCopy(null, ptr, byteCount, 0);
            return (T*)ptr;
        }

        /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Alloc/*'/>
        public static unsafe T* Alloc<T>(int elementCount) where T : unmanaged
        {
            void* ptr = Alloc(elementCount, sizeof(T), out _);
            return (T*)ptr;
        }

        private static unsafe void* Alloc(int elementCount, int sizeInBytes, out long byteCount)
        {
            byteCount = (uint)elementCount * sizeInBytes;
            void* ptr = (void*)Marshal.AllocHGlobal((IntPtr)byteCount);
            GC.AddMemoryPressure(byteCount);
            return ptr;
        }

        /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Free/*'/>
        public static unsafe void Free<T>(T* buffer, int elementCount) where T : unmanaged
        {
            Marshal.FreeHGlobal((IntPtr)buffer);
            GC.RemoveMemoryPressure((uint)elementCount * sizeof(T));
        }

        /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Clear/*'/>
        public static unsafe void Clear<T>(T* buffer, int elementCount) where T : unmanaged
        {
            int size = sizeof(T) * elementCount;
            Buffer.MemoryCopy(null, buffer, size, 0);
        }

        /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Copy/*'/>
        public static unsafe void Copy<T>(T* source, T* destination, int elementCount) where T : unmanaged
        {
            int size = sizeof(T) * elementCount;
            Buffer.MemoryCopy(source, destination, size, size);
        }
    }
}
