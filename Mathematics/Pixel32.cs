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
    public byte this[int index]
    {
        readonly get => index switch
        {
            0 => Red,
            1 => Green,
            2 => Blue,
            3 => Alpha,
            _ => throw new IndexOutOfRangeException($"Index must be in the range [0, 3].")
        };
        set
        {
            switch (index)
            {
                case 0: Red = value; break;
                case 1: Green = value; break;
                case 2: Blue = value; break;
                case 3: Alpha = value; break;
                default: throw new IndexOutOfRangeException($"Index must be in the range [0, 3].");
            }
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

#if NET7_0_OR_GREATER
    private Pixel32(int red, int green, int blue, int alpha)
    {
        Red = byte.CreateSaturating(red);
        Green = byte.CreateSaturating(green);
        Blue = byte.CreateSaturating(blue);
        Alpha = byte.CreateSaturating(alpha);
    }
#endif

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

#if NET7_0_OR_GREATER
    /// <inheritdoc/>
    public readonly bool Equals(Pixel32 other)
    {
        return ((int)this) == (int)other;
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return ((int)this).GetHashCode();
    }
#else
    /// <inheritdoc/>
    public readonly bool Equals(Pixel32 other)
    {
        return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Red, Green, Blue, Alpha);
    }
#endif

    /// <inheritdoc/>
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
    public static Pixel32 Create(byte value)
    {
        return new Pixel32(value, value, value, value);
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

#if NET7_0_OR_GREATER
    static Pixel32 IAdditionOperators<Pixel32, Pixel32, Pixel32>.operator +(Pixel32 left, Pixel32 right)
    {
        return new Pixel32(left.Red + right.Red, left.Green + right.Green, left.Blue + right.Blue, left.Alpha + right.Alpha);
    }

    static Pixel32 IAdditionOperators<Pixel32, byte, Pixel32>.operator +(Pixel32 left, byte right)
    {
        return new Pixel32(left.Red + right, left.Green + right, left.Blue + right, left.Alpha + right);
    }

    static Pixel32 ISubtractionOperators<Pixel32, Pixel32, Pixel32>.operator -(Pixel32 left, Pixel32 right)
    {
        return new Pixel32(left.Red - right.Red, left.Green - right.Green, left.Blue - right.Blue, left.Alpha - right.Alpha);
    }

    static Pixel32 ISubtractionOperators<Pixel32, byte, Pixel32>.operator -(Pixel32 left, byte right)
    {
        return new Pixel32(left.Red - right, left.Green - right, left.Blue - right, left.Alpha - right);
    }

    static Pixel32 IMultiplyOperators<Pixel32, Pixel32, Pixel32>.operator *(Pixel32 left, Pixel32 right)
    {
        return new Pixel32(left.Red * right.Red, left.Green * right.Green, left.Blue * right.Blue, left.Alpha * right.Alpha);
    }

    static Pixel32 IMultiplyOperators<Pixel32, byte, Pixel32>.operator *(Pixel32 left, byte right)
    {
        return new Pixel32(left.Red * right, left.Green * right, left.Blue * right, left.Alpha * right);
    }

    static Pixel32 IDivisionOperators<Pixel32, Pixel32, Pixel32>.operator /(Pixel32 left, Pixel32 right)
    {
        return new Pixel32(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue, left.Alpha / right.Alpha);
    }

    static Pixel32 IDivisionOperators<Pixel32, byte, Pixel32>.operator /(Pixel32 left, byte right)
    {
        return new Pixel32(left.Red / right, left.Green / right, left.Blue / right, left.Alpha / right);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Pixel32"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="value">The <see cref="Pixel32"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(Pixel32 value)
    {
        return Unsafe.BitCast<Pixel32, int>(value);
    }

    /// <summary>
    /// Explicitly converts an <see cref="int"/> to a <see cref="Pixel32"/>.
    /// </summary>
    /// <param name="value">The <see cref="int"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(int value)
    {
        return Unsafe.BitCast<int, Pixel32>(value);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Pixel32"/> to an <see cref="uint"/>.
    /// </summary>
    /// <param name="value">The <see cref="Pixel32"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator uint(Pixel32 value)
    {
        return Unsafe.BitCast<Pixel32, uint>(value);
    }

    /// <summary>
    /// Explicitly converts an <see cref="uint"/> to a <see cref="Pixel32"/>.
    /// </summary>
    /// <param name="value">The <see cref="uint"/> value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(uint value)
    {
        return Unsafe.BitCast<uint, Pixel32>(value);
    }
#endif
}
