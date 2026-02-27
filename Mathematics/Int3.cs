using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a three-dimensional vector with integer X, Y, and Z components.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Int3 : IVector3<Int3, int>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Int3, int, Int3>,
    ISubtractionOperators<Int3, int, Int3>,
    IMultiplyOperators<Int3, int, Int3>,
    IDivisionOperators<Int3, int, Int3>,
    IUnaryPlusOperators<Int3, Int3>,
    IUnaryNegationOperators<Int3, Int3>
#endif
{
    /// <inheritdoc/>
    public static Int3 Zero => default;

    /// <inheritdoc/>
    public static Int3 One { get; } = Create(1);

    /// <inheritdoc/>
    public static Int3 MaxValue { get; } = Create(int.MaxValue);

    /// <inheritdoc/>
    public static Int3 MinValue { get; } = Create(int.MinValue);


    /// <summary>
    /// Initializes a new instance of the <see cref="Int3"/> structure with the specified x, y, and z component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    /// <param name="z">The value to assign to the z component.</param>
    public Int3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <inheritdoc/>
    public int X { readonly get; set; }

    /// <inheritdoc/>
    public int Y { readonly get; set; }

    /// <inheritdoc/>
    public int Z { readonly get; set; }

    /// <inheritdoc/>
    public int this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _ => throw new IndexOutOfRangeException("Index must be in the range [0, 2].")
        };
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                case 2: Z = value; break;
                default: throw new IndexOutOfRangeException("Index must be in the range [0, 2].");
            }
        }
    }

    /// <inheritdoc/>
    public readonly bool Equals(Int3 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        return obj is Int3 other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <inheritdoc/>
    public readonly void Deconstruct(out int x, out int y, out int z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider = null)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
    }

    /// <inheritdoc/>
    public override readonly string ToString() => ToString(null, CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    public static Int3 Create(int value)
    {
        return new Int3(value, value, value);
    }

    /// <inheritdoc/>
    public static bool operator ==(Int3 left, Int3 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Int3 left, Int3 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public static Int3 operator +(Int3 left, Int3 right)
    {
        return new Int3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    /// <inheritdoc/>
    public static Int3 operator +(Int3 left, int right)
    {
        return new Int3(left.X + right, left.Y + right, left.Z + right);
    }

    /// <inheritdoc/>
    public static Int3 operator -(Int3 left, Int3 right)
    {
        return new Int3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    /// <inheritdoc/>
    public static Int3 operator -(Int3 left, int right)
    {
        return new Int3(left.X - right, left.Y - right, left.Z - right);
    }

    /// <inheritdoc/>
    public static Int3 operator *(Int3 left, Int3 right)
    {
        return new Int3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    /// <inheritdoc/>
    public static Int3 operator *(Int3 left, int right)
    {
        return new Int3(left.X * right, left.Y * right, left.Z * right);
    }

    /// <inheritdoc/>
    public static Int3 operator /(Int3 left, Int3 right)
    {
        return new Int3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    /// <inheritdoc/>
    public static Int3 operator /(Int3 left, int right)
    {
        return new Int3(left.X / right, left.Y / right, left.Z / right);
    }

    /// <inheritdoc/>
    public static Int3 operator +(Int3 value)
    {
        return new Int3(+value.X, +value.Y, +value.Z);
    }

    /// <inheritdoc/>
    public static Int3 operator -(Int3 value)
    {
        return new Int3(-value.X, -value.Y, -value.Z);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Int3"/> to a <see cref="Vector3"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Int3"/> value to convert.</param>
    public static implicit operator Vector3(Int3 value)
    {
        return new Vector3(value.X, value.Y, value.Z);
    }
}
