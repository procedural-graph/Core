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
/// Represents a 32-bit vector containing red, green, blue, and alpha (RGBA) color channels, each stored as an 8-bit
/// unsigned integer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Pixel32 : IVector4<Pixel32, byte>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Pixel32, Pixel32, Pixel128>,
    IAdditionOperators<Pixel32, byte, Pixel32>,
    IAdditionOperators<Pixel32, float, Pixel128>,
    ISubtractionOperators<Pixel32, Pixel32, Pixel128>,
    ISubtractionOperators<Pixel32, byte, Pixel32>,
    ISubtractionOperators<Pixel32, float, Pixel128>,
    IMultiplyOperators<Pixel32, Pixel32, Pixel128>,
    IMultiplyOperators<Pixel32, byte, Pixel32>,
    IMultiplyOperators<Pixel32, float, Pixel128>,
    IDivisionOperators<Pixel32, Pixel32, Pixel128>,
    IDivisionOperators<Pixel32, byte, Pixel32>,
    IDivisionOperators<Pixel32, float, Pixel128>
#endif
{
    /// <inheritdoc/>
    public static Pixel32 Zero => default;

    /// <inheritdoc/>
    public static Pixel32 One { get; } = Create(1);

    /// <inheritdoc/>
    public static Pixel32 MaxValue { get; } = Create(byte.MaxValue);

    /// <inheritdoc/>
    public static Pixel32 MinValue => default;

    /// <summary>
    /// Gets or sets the value of the red channel.
    /// </summary>
    public byte Red { readonly get; set; }
    byte IVector4<Pixel32, byte>.X
    {
        readonly get => Red;
        set => Red = value;
    }

    /// <summary>
    /// Gets or sets the value of the green channel.
    /// </summary>
    public byte Green { readonly get; set; }
    byte IVector4<Pixel32, byte>.Y
    {
        readonly get => Green;
        set => Green = value;
    }

    /// <summary>
    /// Gets or sets the value of the blue channel.
    /// </summary>
    public byte Blue { readonly get; set; }
    byte IVector4<Pixel32, byte>.Z
    {
        readonly get => Blue;
        set => Blue = value;
    }

    /// <summary>
    /// Gets or sets the value of the alpha channel.
    /// </summary>
    public byte Alpha { readonly get; set; }
    byte IVector4<Pixel32, byte>.W
    {
        readonly get => Alpha;
        set => Alpha = value;
    }

    /// <inheritdoc/>
    public static int Count => 4;

    private readonly short LengthSquared => ((Int4)this).LengthSquared;
    readonly byte IVector<Pixel32, byte>.LengthSquared => Clamp(LengthSquared);
    readonly float IVector<Pixel32, byte>.Length => FastMath.SqrtEstimate(LengthSquared);

    /// <inheritdoc cref="IVector{TVector, TComponent}.Sum"/>
    public readonly short Sum => ((Int4)this).Sum;
    readonly byte IVector<Pixel32, byte>.Sum => Clamp(Sum);

    /// <inheritdoc/>
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            Utils.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out byte value);
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Utils.ThrowIfOutOfRange(index, Count);
            VectorMath.SetComponent(ref this, index, value);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pixel32"/> structure with the specified red, green, blue and alpha values.
    /// </summary>
    /// <param name="red">The value to assign to the red channel.</param>
    /// <param name="green">The value to assign to the green channel.</param>
    /// <param name="blue">The value to assign to the blue channel.</param>
    /// <param name="alpha">The value to assign to the alpha channel.</param>
    public Pixel32(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Pixel32"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(byte)"/>
    public Pixel32(byte value)
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
    public readonly void Deconstruct(out byte red, out byte green, out byte blue, out byte alpha)
    {
        red = Red;
        green = Green; 
        blue = Blue; 
        alpha = Alpha;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Pixel32 other)
    {
        return ((int)this) == (int)other;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return ((int)this).GetHashCode();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Pixel32 other && Equals(other);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel32 Create(byte value)
    {
        return new Pixel32(value);
    }

    /// <inheritdoc/>
    public static Pixel32 Min(in Pixel32 left, in Pixel32 right)
    {
        return (Pixel32)Int4.Min((Int4)left, (Int4)right);
    }

    /// <inheritdoc/>
    public static Pixel32 Max(in Pixel32 left, in Pixel32 right)
    {
        return (Pixel32)Int4.Max((Int4)left, (Int4)right);
    }

    /// <inheritdoc/>
    public static Pixel32 Clamp(in Pixel32 value, in Pixel32 min, in Pixel32 max)
    {
        return (Pixel32)Int4.Clamp((Int4)value, (Int4)min, (Int4)max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clamp(short value, byte min = byte.MinValue, byte max = byte.MaxValue)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return (byte)value;
    }

#if NET7_0_OR_GREATER
    static byte IVector<Pixel32, byte>.Dot(in Pixel32 left, in Pixel32 right)
    {
        short result = Int4.Dot((Int4)left, (Int4)right);
        return Clamp(result);
    }

    static Pixel32 IVector<Pixel32, byte>.Abs(in Pixel32 vector)
    {
        return vector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IAdditionOperators<Pixel32, Pixel32, Pixel32>.operator +(Pixel32 left, Pixel32 right)
    {
        return (Pixel32)((Int4)left + (Int4)right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IAdditionOperators<Pixel32, byte, Pixel32>.operator +(Pixel32 left, byte right)
    {
        return (Pixel32)((Int4)left + right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 ISubtractionOperators<Pixel32, Pixel32, Pixel32>.operator -(Pixel32 left, Pixel32 right)
    {
        return (Pixel32)((Int4)left - (Int4)right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 ISubtractionOperators<Pixel32, byte, Pixel32>.operator -(Pixel32 left, byte right)
    {
        return (Pixel32)((Int4)left - right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IMultiplyOperators<Pixel32, Pixel32, Pixel32>.operator *(Pixel32 left, Pixel32 right)
    {
        return (Pixel32)((Int4)left * (Int4)right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IMultiplyOperators<Pixel32, byte, Pixel32>.operator *(Pixel32 left, byte right)
    {
        return (Pixel32)((Int4)left * right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IDivisionOperators<Pixel32, Pixel32, Pixel32>.operator /(Pixel32 left, Pixel32 right)
    {
        return (Pixel32)((Int4)left / (Int4)right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Pixel32 IDivisionOperators<Pixel32, byte, Pixel32>.operator /(Pixel32 left, byte right)
    {
        return (Pixel32)((Int4)left / right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Pixel32 left, Pixel32 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Pixel32 left, Pixel32 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel32 left, Pixel32 right)
    {
        return ((Pixel128)left) + ((Pixel128)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel32 left, float right)
    {
        return ((Pixel128)left) + right;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel32 left, Pixel32 right)
    {
        return ((Pixel128)left) - ((Pixel128)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel32 left, float right)
    {
        return ((Pixel128)left) - right;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel32 left, Pixel32 right)
    {
        return ((Pixel128)left) * ((Pixel128)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel32 left, float right)
    {
        return ((Pixel128)left) * right;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel32 left, Pixel32 right)
    {
        return ((Pixel128)left) / ((Pixel128)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel32 left, float right)
    {
        return ((Pixel128)left) / right;
    }
#endif

    /// <summary>
    /// Explicitly converts a <see cref="Pixel32"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="value">The <see cref="Pixel32"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(Pixel32 value)
    {
        return Utils.ReinterpretCast<Pixel32, int>(value);
    }

    /// <summary>
    /// Explicitly converts an <see cref="int"/> to a <see cref="Pixel32"/>.
    /// </summary>
    /// <param name="value">The <see cref="int"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(int value)
    {
        return Utils.ReinterpretCast<int, Pixel32>(value);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Pixel32"/> to an <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The <see cref="Pixel32"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator uint(Pixel32 value)
    {
        return Utils.ReinterpretCast<Pixel32, uint>(value);
    }

    /// <summary>
    /// Explicitly converts an <see cref="uint"/> to a <see cref="Pixel32"/>.
    /// </summary>
    /// <param name="value">The <see cref="uint"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(uint value)
    {
        return Utils.ReinterpretCast<uint, Pixel32>(value);
    }

    /// <summary>
    /// Converts an <see cref="Pixel128"/> to an <see cref="Pixel32"/> by mapping each channel from floating-point to byte
    /// precision.
    /// </summary>
    /// <param name="value">The <see cref="Pixel128"/> instance to convert. Each channel should be in the range 0.0 to 1.0.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(Pixel128 value)
    {
        return (Pixel32)(Int4)(value * 255.0f);
    }
}
