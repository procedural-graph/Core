using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a four-dimensional vector whose components are double-precision floating-point values.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Double4 : IVector4<Double4, double>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Double4, double, Double4>,
    ISubtractionOperators<Double4, double, Double4>,
    IMultiplyOperators<Double4, double, Double4>,
    IDivisionOperators<Double4, double, Double4>,
    IUnaryPlusOperators<Double4, Double4>,
    IUnaryNegationOperators<Double4, Double4>
#endif
{
    /// <inheritdoc/>
    public static int Count => 4;

    /// <inheritdoc/>
    public static Double4 Zero { get; } = new Double4(0.0);

    /// <inheritdoc/>
    public static Double4 One { get; } = new Double4(1.0);

    /// <inheritdoc/>
    public static Double4 MaxValue { get; } = new Double4(double.MaxValue);

    /// <inheritdoc/>
    public static Double4 MinValue { get; } = new Double4(double.MinValue);

    /// <inheritdoc/>
    public double X { get; set; }

    /// <inheritdoc/>
    public double Y { get; set; }

    /// <inheritdoc/>
    public double Z { get; set; }

    /// <inheritdoc/>
    public double W { get; set; }

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
            return X * X + Y * Y + Z * Z + W * W;
        }
    }

    /// <inheritdoc cref="IVector{TSelf, TValue}.Length"/>
    public readonly double Length => FastMath.SqrtEstimate(LengthSquared);
    readonly float IVector<Double4, double>.Length => (float)Length;

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
            return X + Y + Z + W;
        }
    }

    /// <inheritdoc/>
    public double this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out double result);
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
    /// Initializes a new instance of the <see cref="Double4"/> structure with the specified x, y, z, and w component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    /// <param name="z">The value to assign to the z component.</param>
    /// <param name="w">The value to assign to the w component.</param>
    public Double4(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Double4"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(double)"/>
    public Double4(double value)
    {
        X = value;
        Y = value;
        Z = value;
        W = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Create(double value)
    {
        return new Double4(value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Abs(in Double4 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Abs(vector.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double4 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        sResult.Z = Math.Abs(vector.Z);
        sResult.W = Math.Abs(vector.W);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Clamp(in Double4 value, in Double4 min, in Double4 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Clamp(value.AsVector256(), min.AsVector256(), max.AsVector256());
            return FromVector256(in vResult);
        }
        
#endif
        Unsafe.SkipInit(out Double4 sResult);
        sResult.X = double.Clamp(value.X, min.X, max.X);
        sResult.Y = double.Clamp(value.Y, min.Y, max.Y);
        sResult.Z = double.Clamp(value.Z, min.Z, max.Z);
        sResult.W = double.Clamp(value.W, min.W, max.W);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(in Double4 left, in Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector256.Dot(left.AsVector256(), right.AsVector256());
        }

#endif
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Max(in Double4 left, in Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Max(left.AsVector256(), right.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double4 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        sResult.Z = Math.Max(left.Z, right.Z);
        sResult.W = Math.Max(left.W, right.W);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Min(in Double4 left, in Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.Min(left.AsVector256(), right.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double4 sResult);
        sResult.X = Math.Min(left.X, right.X);
        sResult.Y = Math.Min(left.Y, right.Y);
        sResult.Z = Math.Min(left.Z, right.Z);
        sResult.W = Math.Min(left.W, right.W);
        return sResult;
    }

    /// <summary>Transforms a three-dimensional vector by a specified 4x4 matrix.</summary>
    /// <param name="position">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double3 position, Matrix matrix)
    {
        Double4 result = matrix.X * position.X;
        result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
        result = MultiplyAddEstimate(matrix.Z, Create(position.Z), result);
        return result + matrix.W;
    }

    /// <summary>Transforms a three-dimensional vector by the specified Quaternion rotation value.</summary>
    /// <param name="value">The vector to rotate.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double3 value, Quaternion rotation) => Transform((Double4)value, rotation);

    /// <summary>Transforms a four-dimensional vector by a specified 4x4 matrix.</summary>
    /// <param name="vector">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double4 vector, Matrix matrix)
    {
        Double4 result = matrix.X * vector.X;
        result = MultiplyAddEstimate(matrix.Y, Create(vector.Y), result);
        result = MultiplyAddEstimate(matrix.Z, Create(vector.Z), result);
        result = MultiplyAddEstimate(matrix.W, Create(vector.W), result);
        return result;
    }

    /// <summary>Transforms a four-dimensional vector by the specified Quaternion rotation value.</summary>
    /// <param name="value">The vector to rotate.</param>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>The transformed vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 Transform(Double4 value, Quaternion rotation)
    {
        Quaternion conjugate = Quaternion.Conjugate(rotation);
        Quaternion temp = Quaternion.Concatenate(conjugate, (Quaternion)value);
        return (Double4)Quaternion.Concatenate(temp, rotation);
    }

    internal static Double4 MultiplyAddEstimate(Double4 left, Double4 right, Double4 addend)
    {
#if NETCOREAPP3_0_OR_GREATER
         if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = Vector256.MultiplyAddEstimate(left.AsVector256(), right.AsVector256(), addend.AsVector256());
            return FromVector256(in vResult);
        }

#endif
        Unsafe.SkipInit(out Double4 sResult);
        sResult.X = left.X * right.X + addend.X;
        sResult.Y = left.Y * right.Y + addend.Y;
        sResult.Z = left.Z * right.Z + addend.Z;
        sResult.W = left.W * right.W + addend.W;
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out double x, out double y, out double z, out double w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Double4 other)
    {
        Double4 absDifference = Abs(this - other);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> tolerance = Vector256.Create<double>(float.EqualityThreshold);
            return Vector256.LessThanOrEqualAll(absDifference.AsVector256(), tolerance);
        }

#endif
        return absDifference.X <= float.EqualityThreshold 
            && absDifference.Y <= float.EqualityThreshold 
            && absDifference.Z <= float.EqualityThreshold 
            && absDifference.W <= float.EqualityThreshold;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Double4 other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}{separator} {W.ToString(format, formatProvider)}>";
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector256<double> AsVector256()
    {
        return Utils.ReinterpretCast<Double4, Vector256<double>>(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Double4 FromVector256(ref readonly Vector256<double> vector)
    {
        return Utils.ReinterpretCast<Vector256<double>, Double4>(vector);
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator +(Double4 value)
    {
        return value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator +(Double4 left, Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() + right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator +(Double4 left, double right)
    {
        Double4 operand = new(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator -(Double4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = -value.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double4(-value.X, -value.Y, -value.Z, -value.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator -(Double4 left, Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() - right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator -(Double4 left, double right)
    {
        Double4 operand = new(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator *(Double4 left, Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() * right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator *(Double4 left, double right)
    {
        Double4 operand = new(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator /(Double4 left, Double4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vResult = left.AsVector256() / right.AsVector256();
            return FromVector256(in vResult);
        }

#endif
        return new Double4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 operator /(Double4 left, double right)
    {
        return left * FastMath.ReciprocalEstimate(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Double4 left, Double4 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Double4 left, Double4 right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Double4"/> to a <see cref="Quaternion"/>.
    /// </summary>
    /// <param name="value">The <see cref="Double4"/> value to convert.</param>
    /// <returns>A <see cref="Quaternion"/> with components corresponding to the input <see cref="Double4"/>.</returns>
    public static explicit operator Quaternion(Double4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vector = value.AsVector256();
            Vector128<float> narrowed = Vector128.Narrow(vector.GetLower(), vector.GetUpper());
            return Utils.ReinterpretCast<Vector128<float>, Quaternion>(narrowed);
        }

#endif
        return new Quaternion((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Quaternion"/> to a <see cref="Double4"/>.
    /// </summary>
    /// <param name="value">The <see cref="Quaternion"/> value to convert.</param>
    /// <returns>A <see cref="Double4"/> with components corresponding to the input <see cref="Quaternion"/>.</returns>
    public static explicit operator Double4(Quaternion value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<float> vector = Utils.BitCastWrite<Quaternion, Vector256<float>>(value);
            Vector256<double> widened = Vector256.WidenLower(vector);
            return FromVector256(in widened);
        }

#endif
        return new Double4(value.X, value.Y, value.Z, value.W);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Double4"/> to a <see cref="Vector4"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Double4"/> value to convert.</param>
    /// <returns>A <see cref="Vector4"/> with components corresponding to the input <see cref="Double4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector4(Double4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<double> vector = value.AsVector256();
            Vector128<float> narrowed = Vector128.Narrow(vector.GetLower(), vector.GetUpper());
            return Utils.ReinterpretCast<Vector128<float>, Vector4>(narrowed);
        }

#endif
        return new Vector4((float)value.X, (float)value.Y, (float)value.Z, (float)value.W);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector4"/> to a <see cref="Double4"/> by 
    /// converting each component to a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Vector4"/> value to convert.</param>
    /// <returns>A <see cref="Double4"/> with components corresponding to the input <see cref="Vector4"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Double4(Vector4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector256<float> vector = Utils.BitCastWrite<Vector4, Vector256<float>>(value);
            Vector256<double> widened = Vector256.WidenLower(vector);
            return FromVector256(in widened);
        }

#endif
        return new Double4(value.X, value.Y, value.Z, value.W);
    }

    /// <summary>
    /// Explicitly converts a <see cref="Double3"/> to a <see cref="Double4"/>.
    /// </summary>
    /// <param name="value">The <see cref="Double3"/> value to convert.</param>
    /// <returns>
    /// A <see cref="Double4"/> with the x, y, and z components corresponding 
    /// to the input <see cref="Double3"/> and a w component of 1.0.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Double4(Double3 value)
    {
        Double4 result = Utils.ReinterpretCast<Double3, Double4>(value);
        result.W = 1.0;
        return result;
    }
}
