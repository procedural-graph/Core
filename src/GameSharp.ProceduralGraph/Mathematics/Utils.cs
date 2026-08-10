using System.Runtime.CompilerServices;

namespace GameSharp.ProceduralGraph.Mathematics;

internal static class Utils
{
#if !NET7_0_OR_GREATER
    extension(double)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
    extension(int)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
    extension(long)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clamp(long value, long min, long max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
#endif

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
}
