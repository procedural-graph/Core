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
[StructLayout(LayoutKind.Sequential)]
public struct Double2 : IVector2<Double2, double>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Double2, double, Double2>,
    ISubtractionOperators<Double2, double, Double2>,
    IMultiplyOperators<Double2, double, Double2>,
    IDivisionOperators<Double2, double, Double2>,
    IUnaryPlusOperators<Double2, Double2>,
    IUnaryNegationOperators<Double2, Double2>
#endif
{
    /// <inheritdoc/>
    public static Double2 Zero => default;

    /// <inheritdoc/>
    public static Double2 One { get; } = new(1.0);

    /// <inheritdoc/>
    public static Double2 MaxValue { get; } = new(double.MaxValue);

    /// <inheritdoc/>
    public static Double2 MinValue { get; } = new(double.MinValue);

    /// <inheritdoc/>
    public double X { readonly get; set; }

    /// <inheritdoc/>
    public double Y { readonly get; set; }

    /// <inheritdoc/>
    public static int Count => 2;

    /// <inheritdoc/>
    public readonly double LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector128<double> vValue = AsVector128();
                return Vector128.Sum(vValue * vValue);
            }

#endif
            return X * X + Y * Y;
        }
    }

    /// <inheritdoc cref="IVector{TSelf, TComponent}.Length"/>
    public readonly double Length => FastMath.SqrtEstimate(LengthSquared);
    readonly float IVector<Double2, double>.Length => (float)Length;

    /// <inheritdoc/>
    public readonly double Sum
    {
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                return Vector128.Sum(AsVector128());
            }

#endif
            return X + Y;
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
    /// Initializes a new instance of the <see cref="Double2"/> structure with the specified x, y, and z component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    public Double2(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Initializes a new <see cref="Double2"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(double)"/>
    public Double2(double value)
    {
        X = value;
        Y = value;
    }

    /// <inheritdoc/>
    public static Double2 Create(double value)
    {
        return new Double2(value);
    }

    /// <inheritdoc/>
    public readonly void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Double2 other)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.EqualsAll(AsVector128(), other.AsVector128());
        }

#endif
        return X == other.X && Y == other.Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Double2 other && Equals(other);
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
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
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Abs(in Double2 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = Vector128.Abs(vector.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Double2 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Min(in Double2 left, in Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = Vector128.Min(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Double2 sResult);
        sResult.X = Math.Min(left.X, right.X);
        sResult.Y = Math.Min(left.Y, right.Y);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 Max(in Double2 left, in Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = Vector128.Max(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Double2 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        return sResult;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(in Double2 left, in Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.Dot(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X * right.X + left.Y * right.Y;
    }

    /// <inheritdoc/>
    public static Double2 Clamp(in Double2 value, in Double2 min, in Double2 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = Vector128.Clamp(value.AsVector128(), min.AsVector128(), max.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Double2 sResult);
        sResult.X = double.Clamp(value.X, min.X, max.X);
        sResult.Y = double.Clamp(value.Y, min.Y, max.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static bool ApproximatelyEquals(in Double2 left, in Double2 right)
    {
        Double2 absDifference = Abs(left - right);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> tolerance = Vector128.Create<double>(float.EqualityThreshold);
            return Vector128.LessThanOrEqualAll(absDifference.AsVector128(), tolerance);
        }

#endif
        return absDifference.X <= float.EqualityThreshold && absDifference.Y <= float.EqualityThreshold;
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector128<double> AsVector128()
    {
        return Utils.ReinterpretCast<Double2, Vector128<double>>(this);
    }

    private static Double2 FromVector128(Vector128<double> vector)
    {
        return Utils.ReinterpretCast<Vector128<double>, Double2>(vector);
    }
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Double2 left, Double2 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(Double2 left, Double2 right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator +(Double2 left, Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = left.AsVector128() + right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Double2(left.X + right.X, left.Y + right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator +(Double2 left, double right)
    {
        Double2 operand = Create(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator -(Double2 left, Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = left.AsVector128() - right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Double2(left.X - right.X, left.Y - right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator -(Double2 left, double right)
    {
        Double2 operand = Create(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator *(Double2 left, Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = left.AsVector128() * right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Double2(left.X * right.X, left.Y * right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator *(Double2 left, double right)
    {
        Double2 operand = Create(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator /(Double2 left, Double2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = left.AsVector128() / right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Double2(left.X / right.X, left.Y / right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator /(Double2 left, double right)
    {
        double invRight = FastMath.ReciprocalEstimate(right);
        Double2 operand = Create(invRight);
        return left / operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator +(Double2 value)
    {
        return value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 operator -(Double2 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<double> vResult = -value.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Double2(-value.X, -value.Y);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector2"/> to a <see cref="Double2"/> by 
    /// converting each component to a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Vector2"/> value to convert.</param>
    /// <returns>A <see cref="Double2"/> with components corresponding to the input <see cref="Vector2"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2(Double2 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<double> vValue = Utils.ReinterpretCast<Double2, Vector128<double>>(value);
            Vector64<float> vResult = Vector64.Narrow(vValue.GetLower(), vValue.GetUpper());
            return Utils.ReinterpretCast<Vector64<float>, Vector2>(vResult);
        }
#endif
        return new Vector2((float)value.X, (float)value.Y);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Double2"/> to a <see cref="Vector2"/> by 
    /// converting each component to a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The <see cref="Double2"/> value to convert.</param>
    /// <returns>A <see cref="Vector2"/> with components corresponding to the input <see cref="Double2"/>.</returns>
    public static implicit operator Double2(Vector2 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector128.IsHardwareAccelerated)
        {
            Vector128<float> vValue = Utils.ReinterpretCast<Vector2, Vector128<float>>(value);
            Vector128<double> vResult = Vector128.WidenLower(vValue);
            return Utils.ReinterpretCast<Vector128<double>, Double2>(vResult);
        }
#endif
        return new Vector2(value.X, value.Y);
    }
}
