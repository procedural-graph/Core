using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// A static class containing utility methods for vector mathematics.
/// </summary>
public static class VectorMath
{
#if NET7_0_OR_GREATER
    extension<TVector, TComponent>(TVector)
        where TVector : unmanaged, IVector<TVector, TComponent>
        where TComponent : unmanaged, IEquatable<TComponent>
    {
        /// <inheritdoc cref="IVector{TSelf, TComponent}.Clamp(in TSelf, in TSelf, in TSelf)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TVector Clamp(in TVector value, TComponent min, TComponent max)
        {
            TVector minV = TVector.Create(min);
            TVector maxV = TVector.Create(max);
            return TVector.Clamp(in value, in minV, in maxV);
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetComponent<TVector, TComponent>(ref readonly TVector vector, int index, out TComponent component)
        where TVector : unmanaged
        where TComponent : unmanaged
    {
        component = GetComponentRef<TVector, TComponent>(ref Unsafe.AsRef(in vector), index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetComponent<TVector, TComponent>(ref TVector vector, int index, TComponent value)
        where TVector : unmanaged
        where TComponent : unmanaged
    {
        GetComponentRef<TVector, TComponent>(ref vector, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TComponent GetComponentRef<TVector, TComponent>(ref TVector vector, int index)
        where TVector : unmanaged
        where TComponent : unmanaged
    {
        ref TComponent componentRef = ref Unsafe.As<TVector, TComponent>(ref vector);
        return ref Unsafe.Add(ref componentRef, index);
    }
}
