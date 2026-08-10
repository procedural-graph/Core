#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
using System.Runtime.Intrinsics;
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using ProceduralGraph;

namespace GameSharp.ProceduralGraph.Mathematics;

/// <summary>
/// Represents a two-dimensional vector whose components are 32-bit signed integers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Long2 : IVector2<Long2, long>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Long2, long, Long2>,
    ISubtractionOperators<Long2, long, Long2>,
    IMultiplyOperators<Long2, long, Long2>,
    IDivisionOperators<Long2, long, Long2>,
    IUnaryPlusOperators<Long2, Long2>,
    IUnaryNegationOperators<Long2, Long2>
#endif
{
    /// <inheritdoc/>
    public static int Count => 2;

    /// <inheritdoc/>
    public static Long2 Zero => default;

    /// <inheritdoc/>
    public static Long2 One { get; } = new(1);

    /// <inheritdoc/>
    public static Long2 MaxValue { get; } = new(long.MaxValue);

    /// <inheritdoc/>
    public static Long2 MinValue { get; } = new(long.MinValue);

    private long _x;
    /// <inheritdoc/>
    public long X
    {
        readonly get => _x;
        set => _x = value;
    }

    private long _y;
    /// <inheritdoc/>
    public long Y
    {
        readonly get => _y;
        set => _y = value;
    }

    /// <inheritdoc/>
    public readonly long LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector128<long> vValue = AsVector128();
                return Vector128.Sum(vValue * vValue);
            }

#endif
            return X * X + Y * Y;
        }
    }

    /// <inheritdoc/>
    public readonly float Length => FastMath.SqrtEstimate(LengthSquared);

    /// <inheritdoc/>
    public readonly long Sum
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
    public long this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out int value);
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
    /// Initializes a new <see cref="Int2"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(long)"/>
    public Long2(long value)
    {
        _x = value;
        _y = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Int2"/> structure with the specified x and y component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    public Long2(long x, long y)
    {
        _x = x;
        _y = y;
    }

    /// <inheritdoc/>
    public static Long2 Abs(in Long2 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = Vector128.Abs(vector.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Long2 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Long2 Clamp(in Long2 value, in Long2 min, in Long2 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = Vector128.Clamp(value.AsVector128(), min.AsVector128(), max.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Long2 sResult);
        sResult.X = long.Clamp(value.X, min.X, max.X);
        sResult.Y = long.Clamp(value.Y, min.Y, max.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Long2 Create(long value)
    {
        return new Long2(value);
    }

    /// <inheritdoc/>
    public static long Dot(in Long2 left, in Long2 right)
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
    public static Long2 Max(in Long2 left, in Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = Vector128.Max(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Long2 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Long2 Min(in Long2 left, in Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = Vector128.Min(left.AsVector128(), right.AsVector128());
            return FromVector128(vResult);
        }

#endif
        Unsafe.SkipInit(out Long2 sResult);
        sResult.X = Math.Min(left.X, right.X);
        sResult.Y = Math.Min(left.Y, right.Y);
        return sResult;
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
    public static bool LessThanAll(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.LessThanAll(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X < right.X && left.Y < right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is less than or equal to
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Long2, Long2)"/>
    public static bool LessThanOrEqualAll(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAll(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X <= right.X && left.Y <= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Long2, Long2)"/>
    public static bool GreaterThanAll(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAll(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X > right.X && left.Y > right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than or equal to 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Long2, Long2)"/>
    public static bool GreaterThanOrEqualAll(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAll(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X >= right.X && left.Y >= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is less than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Long2, Long2)"/>
    public static bool LessThanAny(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.LessThanAny(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X < right.X || left.Y < right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is less than or equal to
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Long2, Long2)"/>
    public static bool LessThanOrEqualAny(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.LessThanOrEqualAny(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X <= right.X || left.Y <= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is greater than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Long2, Long2)"/>
    public static bool GreaterThanAny(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanAny(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X > right.X || left.Y > right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is greater than or equal to 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Long2, Long2)"/>
    public static bool GreaterThanOrEqualAny(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector128.GreaterThanOrEqualAny(left.AsVector128(), right.AsVector128());
        }

#endif
        return left.X >= right.X || left.Y >= right.Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out long x, out long y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Long2 other)
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
        return obj is Long2 other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector128<long> AsVector128() => Utils.ReinterpretCast<Long2, Vector128<long>>(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Long2 FromVector128(Vector128<long> vector) => Utils.ReinterpretCast<Vector128<long>, Long2>(vector);
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator +(Long2 value)
    {
        return value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator +(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = left.AsVector128() + right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Long2(left.X + right.X, left.Y + right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator +(Long2 left, long right)
    {
        Long2 operand = new(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator -(Long2 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = -value.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Long2(-value.X, -value.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator -(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = left.AsVector128() - right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Long2(left.X - right.X, left.Y - right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator -(Long2 left, long right)
    {
        Long2 operand = new(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator *(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = left.AsVector128() * right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Long2(left.X * right.X, left.Y * right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator *(Long2 left, long right)
    {
        Long2 operand = new(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator /(Long2 left, Long2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<long> vResult = left.AsVector128() / right.AsVector128();
            return FromVector128(vResult);
        }

#endif
        return new Long2(left.X / right.X, left.Y / right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Long2 operator /(Long2 left, long right)
    {
        Long2 operand = new(right);
        return left / operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Long2 left, Long2 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Long2 left, Long2 right)
    {
        return !left.Equals(right);
    }

#if NET7_0_OR_GREATER
    static bool IVector<Long2, long>.ApproximatelyEquals(in Long2 left, in Long2 right)
    {
        return left.Equals(right);
    }
#endif
}
