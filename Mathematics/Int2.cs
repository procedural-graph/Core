using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a two-dimensional vector with integer components.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Int2 : IVector2<Int2, int>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Int2, int, Int2>,
    ISubtractionOperators<Int2, int, Int2>,
    IMultiplyOperators<Int2, int, Int2>,
    IDivisionOperators<Int2, int, Int2>
#endif
{
    /// <inheritdoc/>
    public static Int2 Zero => default;

    /// <inheritdoc/>
    public static Int2 One { get; } = Create(1);

    /// <inheritdoc/>
    public static Int2 MaxValue { get; } = Create(int.MaxValue);

    /// <inheritdoc/>
    public static Int2 MinValue { get; } = Create(int.MinValue);

    /// <inheritdoc/>
    public int X { readonly get; set; }

    /// <inheritdoc/>
    public int Y { readonly get; set; }

    /// <inheritdoc/>
    public int this[int index]
    {
        readonly get => index switch
        {
            0 => X,
            1 => Y,
            _ => throw new IndexOutOfRangeException("Index must be 0 or 1.")
        };
        set
        {
            switch (index)
            {
                case 0: X = value; break;
                case 1: Y = value; break;
                default: throw new IndexOutOfRangeException("Index must be 0 or 1.");
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Int2"/> structure with the specified x, and y component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    public Int2(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc/>
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    public readonly bool Equals(Int2 other)
    {
        return X == other.X && Y == other.Y;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Int2 other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc/>
    public static Int2 Create(int value)
    {
        return new Int2(value, value);
    }

    /// <inheritdoc/>
    public static bool operator ==(Int2 left, Int2 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Int2 left, Int2 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public static Int2 operator +(Int2 left, Int2 right)
    {
        return new Int2(left.X + right.X, left.Y + right.Y);
    }

    /// <inheritdoc/>
    public static Int2 operator +(Int2 left, int right)
    {
        return new Int2(left.X + right, left.Y + right);
    }

    /// <inheritdoc/>
    public static Int2 operator -(Int2 left, Int2 right)
    {
        return new Int2(left.X - right.X, left.Y - right.Y);
    }

    /// <inheritdoc/>
    public static Int2 operator -(Int2 left, int right)
    {
        return new Int2(left.X - right, left.Y - right);
    }

    /// <inheritdoc/>
    public static Int2 operator *(Int2 left, Int2 right)
    {
        return new Int2(left.X * right.X, left.Y * right.Y);
    }

    /// <inheritdoc/>
    public static Int2 operator *(Int2 left, int right)
    {
        return new Int2(left.X * right, left.Y * right);
    }

    /// <inheritdoc/>
    public static Int2 operator /(Int2 left, Int2 right)
    {
        return new Int2(left.X / right.X, left.Y / right.Y);
    }

    /// <inheritdoc/>
    public static Int2 operator /(Int2 left, int right)
    {
        return new Int2(left.X / right, left.Y / right);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Int2"/> to a <see cref="Vector2"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Int2"/> value to convert.</param>
    public static implicit operator Vector2(Int2 value)
    {
        return new Vector2(value.X, value.Y);
    }
}
