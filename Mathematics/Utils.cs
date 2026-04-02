using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics;

internal static class Utils
{
    extension(double)
    {
#if !NET7_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
#endif
    }

    extension(float)
    {
        public static float EqualityThreshold => 1e-5f;

#if !NET7_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo ReinterpretCast<TFrom, TTo>(TFrom value) where TFrom : struct where TTo : struct
    {
#if NET8_0_OR_GREATER
        return Unsafe.BitCast<TFrom, TTo>(value);
#else
        return Unsafe.As<TFrom, TTo>(ref value);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo BitCastWrite<TFrom, TTo>(TFrom value) where TFrom : unmanaged where TTo : unmanaged
    {
        Unsafe.SkipInit(out TTo result);
        ref byte address = ref Unsafe.As<TTo, byte>(ref result);
        Unsafe.WriteUnaligned(ref address, value);
        return result;
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.StackTraceHidden]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfOutOfRange(int index, int count, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if ((uint)index >= count)
        {
            ThrowOutOfRangeException(index, $"Must be a non-negative integer less than {count}.", paramName);
        }
    }

    [DoesNotReturn]
    internal static void ThrowOutOfRangeException(int value, string message, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName, value, message);
    }
}
