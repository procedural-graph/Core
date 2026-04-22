using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a three-dimensional vector whose components are double-precision floating-point values.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32)]
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
    public static int Count => 3;

    /// <inheritdoc/>
    public static Double3 One { get; } = Create(1.0);

    /// <inheritdoc/>
    public static Double3 MaxValue { get; } = Create(double.MaxValue);

    /// <inheritdoc/>
    public static Double3 MinValue { get; } = Create(double.MinValue);

    private double _x;
    /// <inheritdoc/>
    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    private double _y;
    /// <inheritdoc/>
    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    private double _z;
    /// <inheritdoc/>
    public double Z
    {
        readonly get => _z;
        set => _z = value;
    }

    /// <inheritdoc/>
    public readonly double LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector256<double> vector = AsVector256();
                return Vector256.Sum(vector * vector);
            }

#endif
            return X * X + Y * Y + Z * Z;
        }
    }

    /// <inheritdoc cref="IVector{TVector, TComponent}.Length"/>
    public readonly double Length => FastMath.SqrtEstimate(LengthSquared);

    readonly float IVector<Double3, double>.Length => (float)Length;

    /// <inheritdoc/>
    public readonly double Sum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                return Vector256.Sum(AsVector256());
            }

#endif
            return X + Y + Z;
        }
    }

    /// <inheritdoc/>
    public double this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out double value);
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.SetComponent(ref this, index, value);
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Double3"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(double)"/>
    public Double3(double value)
    {
        X = value;
        Y = value;
        Z = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Create(double value)
    {
        return new Double3(value);
    }

    /// <inheritdoc/>
    public readonly void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Double3 other)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.EqualsAll(AsVector256(), other.AsVector256());
        }

#endif
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Abs(in Double3 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Abs(vector.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double3 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        sResult.Z = Math.Abs(vector.Z);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Min(in Double3 left, in Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Min(left.AsVector256(), right.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double3 sResult);
        sResult.X = Math.Min(left.X, right.X);
        sResult.Y = Math.Min(left.Y, right.Y);
        sResult.Z = Math.Min(left.Z, right.Z);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Max(in Double3 left, in Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Max(left.AsVector256(), right.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double3 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        sResult.Z = Math.Max(left.Z, right.Z);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(in Double3 left, in Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.Dot(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Clamp(in Double3 value, in Double3 min, in Double3 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Clamp(value.AsVector256(), min.AsVector256(), max.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double3 sResult);
        sResult.X = double.Clamp(value.X, min.X, max.X);
        sResult.Y = double.Clamp(value.Y, min.Y, max.Y);
        sResult.Z = double.Clamp(value.Z, min.Z, max.Z);
        return sResult;
    }

    /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
    /// <param name="position">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Transform(Double3 position, Matrix matrix) => (Double3)Double4.Transform(position, matrix);

    /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
    /// <param name="value">The vector to rotate.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 Transform(Double3 value, Quaternion rotation) => (Double3)Double4.Transform(value, rotation);

    /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
    /// <param name="normal">The source vector.</param>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 TransformNormal(Double3 normal, Matrix matrix)
    {
        Double4 result = matrix.X * normal.X;
        result = Double4.MultiplyAddEstimate(matrix.Y, Double4.Create(normal.Y), result);
        result = Double4.MultiplyAddEstimate(matrix.Z, Double4.Create(normal.Z), result);
        return (Double3)result;
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector256<double> AsVector256()
    {
        return Utils.ReinterpretCast<Double3, Vector256<double>>(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Double3 FromVector256(ref readonly Vector256<double> vector)
    {
        return Utils.ReinterpretCast<Vector256<double>, Double3>(vector);
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Double3 left, Double3 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Double3 left, Double3 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator +(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() + right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator +(Double3 left, double right)
    {
        Double3 operand = new(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator -(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() - right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator -(Double3 left, double right)
    {
        Double3 operand = new(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator *(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() * right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator *(Double3 left, double right)
    {
        Double3 operand = new(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator /(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() / right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator /(Double3 left, double right)
    {
        return left * FastMath.ReciprocalEstimate(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator +(Double3 value)
    {
        return value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double3 operator -(Double3 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = -value.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double3(-value.X, -value.Y, -value.Z);
    }

    /// <summary>
    /// Performs a component-wise comparison between two vectors.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is less than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool LessThanAll(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.LessThanAll(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X < right.X && left.Y < right.Y && left.Z < right.Z;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is less than or equal to
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Double3, Double3)"/>
    public static bool LessThanOrEqualAll(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.LessThanOrEqualAll(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X <= right.X && left.Y <= right.Y && left.Z <= right.Z;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Double3, Double3)"/>
    public static bool GreaterThanAll(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.GreaterThanAll(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X > right.X && left.Y > right.Y && left.Z > right.Z;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than or equal to 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Double3, Double3)"/>
    public static bool GreaterThanOrEqualAll(Double3 left, Double3 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.GreaterThanOrEqualAll(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X >= right.X && left.Y >= right.Y && left.Z >= right.Z;
    }

    /// <inheritdoc/>
    public static bool ApproximatelyEquals(in Double3 left, in Double3 right)
    {
        Double3 absDifference = Abs(left - right);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> tolerance = Vector256.Create<double>(float.EqualityThreshold);
            return Vector256.LessThanOrEqualAll(absDifference.AsVector256(), tolerance);
        }

#endif
        return absDifference.X <= float.EqualityThreshold
            && absDifference.Y <= float.EqualityThreshold
            && absDifference.Z <= float.EqualityThreshold;
    }

    /// <summary>
    /// Creates a new <see cref="Double2"/> containing the x and y components of this vector.
    /// </summary>
    /// <returns>
    /// A <see cref="Double2"/> with components corresponding to the <see cref="X"/> and <see cref="Y"/> 
    /// components of this vector.
    /// </returns>
    public readonly Double2 MaskXY() => Unsafe.As<double, Double2>(ref Unsafe.AsRef(in _x));

    /// <summary>
    /// Creates a new <see cref="Double2"/> containing the x and z components of this vector.
    /// </summary>
    /// <returns>
    /// A <see cref="Double2"/> with components corresponding to the <see cref="X"/> and <see cref="Z"/> 
    /// components of this vector.
    /// </returns>
    public readonly Double2 MaskXZ() => new(X, Z);

    /// <summary>
    /// Creates a new <see cref="Double2"/> containing the y and z components of this vector.
    /// </summary>
    /// <returns>
    /// A <see cref="Double2"/> with components corresponding to the <see cref="Y"/> and <see cref="Z"/> 
    /// components of this vector.
    /// </returns>
    public readonly Double2 MaskYZ() => Unsafe.As<double, Double2>(ref Unsafe.AsRef(in _y));

    /// <summary>
    /// Explicitly converts a <see cref="Vector3"/> to a <see cref="Double3"/> by 
    /// converting each component to a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Vector3"/> value to convert.</param>
    /// <returns>A <see cref="Double3"/> with components corresponding to the input <see cref="Vector3"/>.</returns>
    public static explicit operator Vector3(Double3 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vValue = value.AsVector256();
            Vector128<float> vResult = Vector128.Narrow(vValue.GetLower(), vValue.GetUpper());
            return Unsafe.As<Vector128<float>, Vector3>(ref vResult);
        }

#endif
        return new Vector3((float)value.X, (float)value.Y, (float)value.Z);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Double3"/> to a <see cref="Vector3"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Double3"/> value to convert.</param>
    /// <returns>A <see cref="Vector3"/> with components corresponding to the input <see cref="Double3"/>.</returns>
    public static implicit operator Double3(Vector3 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<float> vValue = Utils.BitCastWrite<Vector3, Vector256<float>>(value);
            Vector256<double> vResult = Vector256.WidenLower(vValue);
            return FromVector256(in vResult);
        }

#endif
        return new Double3(value.X, value.Y, value.Z);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Double4"/> to a <see cref="Double3"/>.
    /// </summary>
    /// <param name="value">The <see cref="Double4"/> value to convert.</param>
    /// <returns>A <see cref="Double3"/> with components corresponding to the input <see cref="Double4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Double3(Double4 value)
    {
        return Utils.ReinterpretCast<Double4, Double3>(value);
    }
}
