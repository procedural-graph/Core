using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a two-dimensional vector with integer components.
/// </summary>
public unsafe struct Int2 : IVector2<Int2, int>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Int2, int, Int2>,
    ISubtractionOperators<Int2, int, Int2>,
    IMultiplyOperators<Int2, int, Int2>,
    IDivisionOperators<Int2, int, Int2>
#endif
{
    private const int ComponentCount = 2;

    /// <inheritdoc/>
    public static Int2 Zero => default;

    /// <inheritdoc/>
    public static Int2 One { get; } = Create(1);

    /// <inheritdoc/>
    public static Int2 MaxValue { get; } = Create(int.MaxValue);

    /// <inheritdoc/>
    public static Int2 MinValue { get; } = Create(int.MinValue);

    private fixed int _values[ComponentCount];

    /// <inheritdoc/>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than 2.</exception>
    public ref int this[int index]
    {
        get
        {
            if ((uint)index >= ComponentCount)
            {
                throw new IndexOutOfRangeException("Index must be in the range [0, 2].");
            }

            fixed (int* ptr = _values)
            {
                return ref ptr[index];
            }
        }
    }

    /// <inheritdoc/>
    public int X
    {
        readonly get => _values[0];
        set => _values[0] = value;
    }

    /// <inheritdoc/>
    public int Y
    {
        readonly get => _values[1];
        set => _values[1] = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Int2"/> structure with the specified x, and y component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    public Int2(int x, int y)
    {
        _values[0] = x;
        _values[1] = y;
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
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public static unsafe Int2 Create(int value)
    {
        Int2 result = default;
        int* ptr = result._values;
        ptr[0] = value;
        ptr[1] = value;
        return result;
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
