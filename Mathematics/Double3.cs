using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a three-dimensional vector whose components are double-precision floating-point values.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Double3 : IVector3<Double3, double>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Double3, double, Double3>,
    ISubtractionOperators<Double3, double, Double3>,
    IMultiplyOperators<Double3, double, Double3>,
    IDivisionOperators<Double3, double, Double3>,
    IUnaryPlusOperators<Double3, Double3>,
    IUnaryNegationOperators<Double3, Double3>
#endif
{
    /// <inheritdoc/>
    public static Double3 Zero => default;

    /// <inheritdoc/>
    public static Double3 One { get; } = Create(1.0);

    /// <inheritdoc/>
    public static Double3 MaxValue { get; } = Create(double.MaxValue);

    /// <inheritdoc/>
    public static Double3 MinValue { get; } = Create(double.MinValue);

    /// <inheritdoc/>
    public double X { readonly get; set; }

    /// <inheritdoc/>
    public double Y { readonly get; set; }

    /// <inheritdoc/>
    public double Z { readonly get; set; }

    /// <inheritdoc/>
    public double this[int index]
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Double3"/> structure with the specified x, y, and z component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    /// <param name="z">The value to assign to the z component.</param>
    public Double3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <inheritdoc/>
    public static Double3 Create(double value)
    {
        return new Double3(value, value, value);
    }

    /// <inheritdoc/>
    public readonly void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <inheritdoc/>
    public readonly bool Equals(Double3 other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Double3 other && Equals(other);
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    /// <inheritdoc/>
    public static bool operator ==(Double3 left, Double3 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Double3 left, Double3 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public static Double3 operator +(Double3 left, Double3 right)
    {
        return new Double3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    /// <inheritdoc/>
    public static Double3 operator +(Double3 left, double right)
    {
        return new Double3(left.X + right, left.Y + right, left.Z + right);
    }

    /// <inheritdoc/>
    public static Double3 operator -(Double3 left, Double3 right)
    {
        return new Double3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    /// <inheritdoc/>
    public static Double3 operator -(Double3 left, double right)
    {
        return new Double3(left.X - right, left.Y - right, left.Z - right);
    }

    /// <inheritdoc/>
    public static Double3 operator *(Double3 left, Double3 right)
    {
        return new Double3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    /// <inheritdoc/>
    public static Double3 operator *(Double3 left, double right)
    {
        return new Double3(left.X * right, left.Y * right, left.Z * right);
    }

    /// <inheritdoc/>
    public static Double3 operator /(Double3 left, Double3 right)
    {
        return new Double3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    /// <inheritdoc/>
    public static Double3 operator /(Double3 left, double right)
    {
        return new Double3(left.X / right, left.Y / right, left.Z / right);
    }

    /// <inheritdoc/>
    public static Double3 operator +(Double3 value)
    {
        return new Double3(value.X, value.Y, value.Z);
    }

    /// <inheritdoc/>
    public static Double3 operator -(Double3 value)
    {
        return new Double3(-value.X, -value.Y, -value.Z);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector3"/> to a <see cref="Double3"/> by 
    /// converting each component to a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Vector3"/> value to convert.</param>
    /// <returns>A <see cref="Double3"/> with components corresponding to the input <see cref="Vector3"/>.</returns>
    public static explicit operator Vector3(Double3 value) => new((float)value.X, (float)value.Y, (float)value.Z);

    /// <summary>
    /// Implicitly converts a <see cref="Double3"/> to a <see cref="Vector3"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Double3"/> value to convert.</param>
    /// <returns>A <see cref="Vector3"/> with components corresponding to the input <see cref="Double3"/>.</returns>
    public static implicit operator Double3(Vector3 value) => new(value.X, value.Y, value.Z);
}
