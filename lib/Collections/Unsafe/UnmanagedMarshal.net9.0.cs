using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Collections.Unsafe;

public static partial class UnmanagedMarshal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe T* AllocZeroed<T>(int elementCount) where T : unmanaged
    {
        return (T*)NativeMemory.AllocZeroed((nuint)elementCount, (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe T* Alloc<T>(int elementCount) where T : unmanaged
    {
        return (T*)NativeMemory.Alloc((nuint)elementCount, (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void Free<T>(T* buffer) where T : unmanaged
    {
        NativeMemory.Free(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void Clear<T>(T* buffer, int elementCount) where T : unmanaged
    {
        NativeMemory.Clear(buffer, (nuint)elementCount * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void Copy<T>(T* source, T* destination, int elementCount) where T : unmanaged
    {
        NativeMemory.Copy(source, destination, (nuint)elementCount * (nuint)sizeof(T));
    }
}
