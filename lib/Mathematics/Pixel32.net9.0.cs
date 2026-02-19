using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics;

public partial struct Pixel32 : IAdditionOperators<Pixel32, Pixel32, Pixel128>,
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
{
    private unsafe Pixel32(int red, int green, int blue, int alpha)
    {
        _values[0] = byte.CreateSaturating(red);
        _values[1] = byte.CreateSaturating(green);
        _values[2] = byte.CreateSaturating(blue);
        _values[3] = byte.CreateSaturating(alpha);
    }

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
}
