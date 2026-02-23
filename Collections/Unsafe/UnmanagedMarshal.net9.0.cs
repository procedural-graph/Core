using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Collections.Unsafe;

public static partial class UnmanagedMarshal
{
    /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/AllocZeroed/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* AllocZeroed<T>(int elementCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.AllocZeroed, elementCount);
    }

    /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Alloc/*'/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Alloc<T>(int elementCount) where T : unmanaged
    {
        return Alloc<T>(&NativeMemory.Alloc, elementCount);
    }

    private static unsafe T* Alloc<T>(delegate*<nuint, void*> funcPtr, int elementCount) where T : unmanaged
    {
        long byteCount = (uint)elementCount * sizeof(T);
        T* ptr = (T*)funcPtr((nuint)byteCount);
        GC.AddMemoryPressure(byteCount);
        return ptr;
    }

    /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Free/*'/>
    public static unsafe void Free<T>(T* buffer, int elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
        NativeMemory.Free(buffer);
        GC.RemoveMemoryPressure((uint)elementCount * sizeof(T));
    }

    /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Clear/*'/>
    public static unsafe void Clear<T>(T* buffer, int elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
        NativeMemory.Clear(buffer, (nuint)elementCount * (nuint)sizeof(T));
    }

    /// <include file='UnmanagedMarshal.cs.xml' path='doc/members[@name="UnmanagedMarshal"]/Copy/*'/>
    public static unsafe void Copy<T>(T* source, T* destination, int elementCount) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));
        NativeMemory.Copy(source, destination, (nuint)((uint)elementCount * sizeof(T)));
    }
}
