using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET7_0_OR_GREATER
using System.Numerics;
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
    public static Pixel128 One { get; } = Create(1.0f);

    /// <inheritdoc/>
    public static Pixel128 MaxValue { get; } = Create(float.MaxValue);

    /// <inheritdoc/>
    public static Pixel128 MinValue { get; } = Create(float.MinValue);

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
    public float this[int index]
    {
        readonly get => index switch
        {
            0 => Red,
            1 => Green,
            2 => Blue,
            3 => Alpha,
            _ => throw new IndexOutOfRangeException("Index must be in the range [0, 3].")
        };
        set
        {
            switch (index)
            {
                case 0: Red = value; break;
                case 1: Green = value; break;
                case 2: Blue = value; break;
                case 3: Alpha = value; break;
                default: throw new IndexOutOfRangeException("Index must be in the range [0, 3].");
            }
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
        return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
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
    public static unsafe Pixel128 Create(float value)
    {
        return new Pixel128(value, value, value, value);
    }

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

#if NET7_0_OR_GREATER
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) + ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, float right)
    {
        return ((Vector4)left) + Vector4.Create(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) - ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, float right)
    {
        return ((Vector4)left) - Vector4.Create(right);
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
        return ((Vector4)left) * Vector4.Create(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) / ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, float right)
    {
        return ((Vector4)left) / Vector4.Create(right);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Pixel128"/> value to a <see cref="Vector4"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Pixel128"/> value to convert to a <see cref="Vector4"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(Pixel128 value)
    {
        return Unsafe.BitCast<Pixel128, Vector4>(value);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector4"/> value to a <see cref="Pixel128"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Vector4"/> value to convert to a <see cref="Pixel128"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel128(Vector4 value)
    {
        return Unsafe.BitCast<Vector4, Pixel128>(value);
    }
#else
    /// <inheritdoc/>
    public static Pixel128 operator +(Pixel128 left, Pixel128 right)
    {
        return new Pixel128(left.Red + right.Red, left.Green + right.Green, left.Blue + right.Blue, left.Alpha + right.Alpha);
    }

    /// <inheritdoc/>
    public static Pixel128 operator +(Pixel128 left, float right)
    {
        return new Pixel128(left.Red + right, left.Green + right, left.Blue + right, left.Alpha + right);
    }

    /// <inheritdoc/>
    public static Pixel128 operator -(Pixel128 left, Pixel128 right)
    {
        return new Pixel128(left.Red - right.Red, left.Green - right.Green, left.Blue - right.Blue, left.Alpha - right.Alpha);
    }

    /// <inheritdoc/>
    public static Pixel128 operator -(Pixel128 left, float right)
    {
        return new Pixel128(left.Red - right, left.Green - right, left.Blue - right, left.Alpha - right);
    }

    /// <inheritdoc/>
    public static Pixel128 operator *(Pixel128 left, Pixel128 right)
    {
        return new Pixel128(left.Red * right.Red, left.Green * right.Green, left.Blue * right.Blue, left.Alpha * right.Alpha);
    }

    /// <inheritdoc/>
    public static Pixel128 operator *(Pixel128 left, float right)
    {
        return new Pixel128(left.Red * right, left.Green * right, left.Blue * right, left.Alpha * right);
    }

    /// <inheritdoc/>
    public static Pixel128 operator /(Pixel128 left, Pixel128 right)
    {
        return new Pixel128(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue, left.Alpha / right.Alpha);
    }

    /// <inheritdoc/>
    public static Pixel128 operator /(Pixel128 left, float right)
    {
        return new Pixel128(left.Red / right, left.Green / right, left.Blue / right, left.Alpha / right);
    }
#endif

    /// <summary>
    /// Converts an <see cref="Pixel32"/> to an <see cref="Pixel128"/> by normalizing each channel to the range
    /// 0.0 to 1.0.
    /// </summary>
    /// <param name="value">The 32-bit RGBA color value to convert.</param>
    public static implicit operator Pixel128(Pixel32 value)
    {
        const float scale = 1.0f / 255.0f;
        return new Pixel128(value.Red * scale, value.Green * scale, value.Blue * scale, value.Alpha * scale);
    }

    /// <summary>
    /// Converts an <see cref="Pixel128"/> to an <see cref="Pixel32"/> by mapping each channel from floating-point to byte
    /// precision.
    /// </summary>
    /// <param name="value">The <see cref="Pixel128"/> instance to convert. Each channel should be in the range 0.0 to 1.0.</param>
    public static explicit operator Pixel32(Pixel128 value)
    {
        float maxValue = byte.MaxValue;
        return new Pixel32((byte)(value.Red * maxValue), (byte)(value.Green * maxValue), (byte)(value.Blue * maxValue), (byte)(value.Alpha * maxValue));
    }
}
