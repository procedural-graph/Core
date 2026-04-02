using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a 128-bit vector containing red, green, blue, and alpha (RGBA) color channels, each stored as an 32-bit
/// floating point number.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Pixel128 : IVector4<Pixel128, float>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Pixel128, float, Pixel128>,
    ISubtractionOperators<Pixel128, float, Pixel128>,
    IMultiplyOperators<Pixel128, float, Pixel128>,
    IDivisionOperators<Pixel128, float, Pixel128>
#endif
{
    /// <inheritdoc/>
    public static Pixel128 Zero => default;

    /// <inheritdoc/>
    public static Pixel128 One { get; } = new(1.0f);

    /// <inheritdoc/>
    public static Pixel128 MaxValue { get; } = new(float.MaxValue);

    /// <inheritdoc/>
    public static Pixel128 MinValue { get; } = new(float.MinValue);

    /// <summary>
    /// Gets or sets the value of the red channel.
    /// </summary>
    public float Red { readonly get; set; }
    float IVector4<Pixel128, float>.X
    {
        readonly get => Red;
        set => Red = value;
    }

    /// <summary>
    /// Gets or sets the value of the green channel.
    /// </summary>
    public float Green { readonly get; set; }
    float IVector4<Pixel128, float>.Y
    {
        readonly get => Green;
        set => Green = value;
    }

    /// <summary>
    /// Gets or sets the value of the blue channel.
    /// </summary>
    public float Blue { readonly get; set; }
    float IVector4<Pixel128, float>.Z
    {
        readonly get => Blue;
        set => Blue = value;
    }

    /// <summary>
    /// Gets or sets the value of the alpha channel.
    /// </summary>
    public float Alpha { readonly get; set; }
    float IVector4<Pixel128, float>.W
    {
        readonly get => Alpha;
        set => Alpha = value;
    }

    /// <inheritdoc/>
    public static int Count => 4;

    private readonly float LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector128<float> vector = AsVector128();
                return Vector128.Sum(vector * vector);
            }

#endif
            return Red * Red + Green * Green + Blue * Blue + Alpha * Alpha;
        }
    }
    readonly float IVector<Pixel128, float>.LengthSquared => LengthSquared;

    readonly float IVector<Pixel128, float>.Length => FastMath.SqrtEstimate(LengthSquared);

    public readonly float Sum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                return Vector128.Sum(AsVector128());
            }

#endif
            return Red + Green + Blue + Alpha;
        }
    }

    /// <inheritdoc/>
    public float this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out float result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.SetComponent(ref this, index, value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pixel128"/> structure with the specified red, green, blue and alpha values.
    /// </summary>
    /// <param name="red">The value to assign to the red channel.</param>
    /// <param name="green">The value to assign to the green channel.</param>
    /// <param name="blue">The value to assign to the blue channel.</param>
    /// <param name="alpha">The value to assign to the alpha channel.</param>
    public Pixel128(float red, float green, float blue, float alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pixel128"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(float)"/>
    public Pixel128(float value)
    {
        Red = value;
        Green = value;
        Blue = value;
        Alpha = value;
    }

    /// <summary>
    /// Deconstructs the instance into its red, green, blue, and alpha channel values.
    /// </summary>
    /// <param name="red">When this method returns, contains the value of the red channel.</param>
    /// <param name="green">When this method returns, contains the value of the green channel.</param>
    /// <param name="blue">When this method returns, contains the value of the blue channel.</param>
    /// <param name="alpha">When this method returns, contains the value of the alpha channel.</param>
    public readonly void Deconstruct(out float red, out float green, out float blue, out float alpha)
    {
        red = Red;
        green = Green; 
        blue = Blue; 
        alpha = Alpha;
    }

    /// <inheritdoc/>
    public readonly bool Equals(Pixel128 other)
    {
        Pixel128 absDifference = Abs(this - other);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> tolerance = Vector128.Create(float.EqualityThreshold);
            return Vector128.LessThanOrEqualAll(absDifference.AsVector128(), tolerance);
        }

#endif
        return absDifference.Red <= float.EqualityThreshold &&
               absDifference.Green <= float.EqualityThreshold &&
               absDifference.Blue <= float.EqualityThreshold &&
               absDifference.Alpha <= float.EqualityThreshold;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Pixel128 other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Red, Green, Blue, Alpha);
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{Red.ToString(format, formatProvider)}{separator} {Green.ToString(format, formatProvider)}{separator} {Blue.ToString(format, formatProvider)}{separator} {Alpha.ToString(format, formatProvider)}>";
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc/>
    public static Pixel128 Create(float value)
    {
        return new Pixel128(value);
    }

    /// <inheritdoc/>
    public static Pixel128 Abs(in Pixel128 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = Vector128.Abs(vector.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Pixel128 sResult);
        sResult.Red = Math.Abs(vector.Red);
        sResult.Green = Math.Abs(vector.Green);
        sResult.Blue = Math.Abs(vector.Blue);
        sResult.Alpha = Math.Abs(vector.Alpha);
        return sResult;
    }

    /// <inheritdoc/>
    public static Pixel128 Min(in Pixel128 left, in Pixel128 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = Vector128.Min(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Pixel128 sResult);
        sResult.Red = Math.Min(left.Red, right.Red);
        sResult.Green = Math.Min(left.Green, right.Green);
        sResult.Blue = Math.Min(left.Blue, right.Blue);
        sResult.Alpha = Math.Min(left.Alpha, right.Alpha);
        return sResult;
    }

    /// <inheritdoc/>
    public static Pixel128 Max(in Pixel128 left, in Pixel128 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = Vector128.Max(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Pixel128 sResult);
        sResult.Red = Math.Max(left.Red, right.Red);
        sResult.Green = Math.Max(left.Green, right.Green);
        sResult.Blue = Math.Max(left.Blue, right.Blue);
        sResult.Alpha = Math.Max(left.Alpha, right.Alpha);
        return sResult;
    }

    /// <inheritdoc/>
    public static Pixel128 Clamp(in Pixel128 value, in Pixel128 min, in Pixel128 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = Vector128.Clamp(value.AsVector128(), min.AsVector128(), max.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Pixel128 sResult);
        sResult.Red = float.Clamp(value.Red, min.Red, max.Red);
        sResult.Green = float.Clamp(value.Green, min.Green, max.Green);
        sResult.Blue = float.Clamp(value.Blue, min.Blue, max.Blue);
        sResult.Alpha = float.Clamp(value.Alpha, min.Alpha, max.Alpha);
        return sResult;
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector128<float> AsVector128()
    {
        return Utils.ReinterpretCast<Pixel128, Vector128<float>>(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Pixel128 FromVector128(Vector128<float> vector)
    {
        return Utils.ReinterpretCast<Vector128<float>, Pixel128>(vector);
    }
#endif

#if NET7_0_OR_GREATER
    static float IVector<Pixel128, float>.Dot(in Pixel128 left, in Pixel128 right)
    {
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.Dot(left.AsVector128(), right.AsVector128());
        }

        return left.Red * right.Red + left.Green * right.Green + left.Blue * right.Blue + left.Alpha * right.Alpha;
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Pixel128 left, Pixel128 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Pixel128 left, Pixel128 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, Pixel128 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = left.AsVector128() + right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue, left.Alpha / right.Alpha);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, float right)
    {
        Pixel128 operand = new(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, Pixel128 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = left.AsVector128() - right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new(left.Red - right.Red, left.Green - right.Green, left.Blue - right.Blue, left.Alpha - right.Alpha);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, float right)
    {
        Pixel128 operand = new(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) * ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel128 left, float right)
    {
        Pixel128 operand = new(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, Pixel128 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vResult = left.AsVector128() / right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue, left.Alpha / right.Alpha);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, float right)
    {
        float reciprocal = FastMath.ReciprocalEstimate(right);
        return left * reciprocal;
    }

    /// <summary>
    /// Implicitly converts a <see cref="Pixel128"/> value to a <see cref="Vector4"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Pixel128"/> value to convert to a <see cref="Vector4"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(Pixel128 value)
    {
        return Utils.ReinterpretCast<Pixel128, Vector4>(value);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector4"/> value to a <see cref="Pixel128"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Vector4"/> value to convert to a <see cref="Pixel128"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel128(Vector4 value)
    {
        return Utils.ReinterpretCast<Vector4, Pixel128>(value);
    }

    /// <summary>
    /// Converts an <see cref="Pixel32"/> to an <see cref="Pixel128"/> by normalizing each channel to the range
    /// 0.0 to 1.0.
    /// </summary>
    /// <param name="value">The 32-bit RGBA color value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel128(Pixel32 value)
    {
        const float reciprocal = 1.0f / 255.0f;
        return ((Pixel128)(Int4)value) * reciprocal;
    }
}
