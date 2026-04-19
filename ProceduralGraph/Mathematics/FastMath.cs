using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Provides methods for fast approximations of mathematical functions.
/// </summary>
public static class FastMath
{
    /// <summary>
    /// Estimates the square root of the specified single-precision floating-point value.
    /// </summary>
    /// <param name="value">The value for which to estimate the square root.</param>
    /// <returns>An estimated square root of the specified value.</returns>
    /// <inheritdoc cref="ReciprocalSqrtEstimate(float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SqrtEstimate(float value)
    {
        return value * ReciprocalSqrtEstimate(value);
    }

    /// <summary>
    /// Estimates the reciprocal of the square root of the specified floating-point value.
    /// </summary>
    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>x86/64 (.NET Core 3+)</term>
    /// <description>May use the <c>RSQRTSS</c> instruction.</description>
    /// </item>
    /// <item>
    /// <term>ARM (.NET Core 3+)</term>
    /// <description>May use the <c>FRSQRTE</c> instruction, which performs a single Newton-Raphson iteration.</description>
    /// </item>
    /// <item>
    /// <term>Other platforms</term>
    /// <description>
    /// No hardware acceleration; uses a 
    /// <see href="https://en.wikipedia.org/wiki/Fast_inverse_square_root#Overview_of_the_code">
    /// fast bit-level approximation algorithm</see>.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="value">The value for which to estimate the reciprocal square root.</param>
    /// <returns>An approximation of 1 divided by the square root of <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReciprocalSqrtEstimate(float value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (AdvSimd.IsSupported)
        {
            Vector128<float> vector = Vector128.CreateScalar(value);
            Vector128<float> estimate = AdvSimd.ReciprocalSquareRootEstimate(vector);
            return Vector128.ToScalar(estimate);
        }

        if (Sse.IsSupported)
        {
            Vector128<float> vector = Vector128.CreateScalar(value);
            vector = Sse.ReciprocalSqrtScalar(vector);
            return Vector128.ToScalar(vector);
        }

#endif
        return SoftwareReciprocalSqrtEstimate(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SoftwareReciprocalSqrtEstimate(float value)
    {
        int i = Utils.ReinterpretCast<float, int>(value);
        i = (0x5F3759DF - ((i & 0x7FFFFFFF) >> 1)) | (i & unchecked((int)0x80000000));
        float estimate = Utils.ReinterpretCast<int, float>(i);
        return estimate * (1.5f - 0.5f * value * estimate * estimate);
    }

    /// <summary>
    /// Estimates the reciprocal of the specified single-precision floating-point value.
    /// </summary>
    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>x86/64 (.NET 6+)</term>
    /// <description>May use the <c>RCPSS</c> instruction.</description>
    /// </item>
    /// <item>
    /// <term>ARM (.NET 6+)</term>
    /// <description>May use the <c>FRECPE</c> instruction.</description>
    /// </item>
    /// <item>
    /// <term>Other platforms</term>
    /// <description>
    /// No hardware acceleration; uses floating-point division.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="value">The value for which to estimate the reciprocal.</param>
    /// <returns>1 divided by <paramref name="value"/> or a close approximation thereof.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReciprocalEstimate(float value)
    {
#if NET6_0_OR_GREATER
        return System.MathF.ReciprocalEstimate(value);
#else
        return 1.0f / value;
#endif
    }

    /// <summary>
    /// Estimates the square root of the specified double-precision floating-point value.
    /// </summary>
    /// <param name="value">The value for which to estimate the square root.</param>
    /// <returns>An estimated square root of the specified value.</returns>
    /// <inheritdoc cref="ReciprocalSqrtEstimate(double)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SqrtEstimate(double value)
    {
        return value * ReciprocalSqrtEstimate(value);
    }

    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>x86/64 (.NET 8+)</term>
    /// <description>May use the <c>VRSQRT14SD</c> instruction.</description>
    /// </item>
    /// <item>
    /// <term>ARM (.NET Core 3+)</term>
    /// <description>May use the <c>FRSQRTE</c> instruction, which performs a single Newton-Raphson iteration.</description>
    /// </item>
    /// <item>
    /// <term>Other platforms</term>
    /// <description>
    /// No hardware acceleration; uses a 
    /// <see href="https://en.wikipedia.org/wiki/Fast_inverse_square_root#Overview_of_the_code">
    /// fast bit-level approximation algorithm</see>.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <inheritdoc cref="ReciprocalSqrtEstimate(float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReciprocalSqrtEstimate(double value)
    {
#if NET8_0_OR_GREATER
        if (Avx512F.IsSupported)
        {
            Vector512<double> vector = Vector512.CreateScalar(value);
            Vector512<double> estimate = Avx512F.ReciprocalSqrt14(vector);
            return Vector512.ToScalar(estimate);
        }

#endif
#if NETCOREAPP3_0_OR_GREATER
        if (AdvSimd.Arm64.IsSupported)
        {
            Vector64<double> vector = Vector64.CreateScalar(value);
            Vector64<double> estimate = AdvSimd.Arm64.ReciprocalSquareRootEstimateScalar(vector);
            return Vector64.ToScalar(estimate);
        }

#endif
        return SoftwareReciprocalSqrtEstimate(value);
    }

    private static double SoftwareReciprocalSqrtEstimate(double value)
    {
        long i = Utils.ReinterpretCast<double, long>(value);
        i = (0x5FE6EB50C7B537A9 - ((i & 0x7FFFFFFFFFFFFFFF) >> 1)) | (i & unchecked((long)0x8000000000000000));
        double estimate = Utils.ReinterpretCast<long, double>(i);
        return estimate * (1.5 - 0.5 * value * estimate * estimate);
    }

    /// <summary>
    /// Estimates the reciprocal of the specified double-precision floating-point value.
    /// </summary>
    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>x86/64 (.NET 8+)</term>
    /// <description>May use the <c>VRCP14SD</c> instruction.</description>
    /// </item>
    /// <item>
    /// <term>ARM64 (.NET 6+)</term>
    /// <description>May use use the <c>FRECPE</c> instruction which performs a single Newton-Raphson iteration.</description>
    /// </item>
    /// <item>
    /// <term>Other platforms</term>
    /// <description>
    /// No hardware acceleration; uses floating-point division.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="value">The value for which to estimate the reciprocal.</param>
    /// <returns>1 divided by <paramref name="value"/> or a close approximation thereof.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReciprocalEstimate(double value)
    {
#if NET8_0_OR_GREATER
        if (Avx512F.IsSupported)
        {
            Vector512<double> vector = Vector512.CreateScalar(value);
            Vector512<double> estimate = Avx512F.Reciprocal14(vector);
            return Vector512.ToScalar(estimate);
        }
#endif
#if NET6_0_OR_GREATER
         return System.Math.ReciprocalEstimate(value);
#else
         return 1.0 / value;
#endif
    }
}
