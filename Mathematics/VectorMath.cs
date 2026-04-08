using System.Numerics;
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

    extension (Vector3)
    {
        /// <inheritdoc cref="IVector{TSelf, TValue}.ApproximatelyEquals(in TSelf, in TSelf)"/>
        public static bool ApproximatelyEquals(in Vector3 left, in Vector3 right)
        {
            Vector3 absDifference = Vector3.Abs(left - right);
#if NET9_0_OR_GREATER
            Vector3 tolerance = Vector3.Create(float.EqualityThreshold);
            return Vector3.LessThanAll(absDifference, tolerance);
#else
            return absDifference.X < float.EqualityThreshold 
                && absDifference.Y < float.EqualityThreshold 
                && absDifference.Z < float.EqualityThreshold;
#endif
        }
    }

    extension (Vector4)
    {
        /// <inheritdoc cref="IVector{TSelf, TValue}.ApproximatelyEquals(in TSelf, in TSelf)"/>
        public static bool ApproximatelyEquals(in Vector4 left, in Vector4 right)
        {
            Vector4 absDifference = Vector4.Abs(left - right);
#if NET9_0_OR_GREATER
            Vector4 tolerance = Vector4.Create(float.EqualityThreshold);
            return Vector4.LessThanAll(absDifference, tolerance);
#else
            return absDifference.X < float.EqualityThreshold 
                && absDifference.Y < float.EqualityThreshold 
                && absDifference.Z < float.EqualityThreshold
                && absDifference.W < float.EqualityThreshold;
#endif
        }
    }

    extension (Quaternion)
    {
        /// <inheritdoc cref="IVector{TSelf, TValue}.ApproximatelyEquals(in TSelf, in TSelf)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ApproximatelyEquals(in Quaternion left, in Quaternion right)
        {
#if NET9_0_OR_GREATER
            return Vector4.ApproximatelyEquals(left.AsVector4(), right.AsVector4());
#else
            return Vector4.ApproximatelyEquals(new(left.X, left.Y, left.Z, left.W), new(right.X, right.Y, right.Z, right.W));
#endif
        }
    }

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
