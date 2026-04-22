#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
using System.Runtime.Intrinsics;
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a two-dimensional vector whose components are 32-bit signed integers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Int2 : IVector2<Int2, int>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Int2, int, Int2>,
    ISubtractionOperators<Int2, int, Int2>,
    IMultiplyOperators<Int2, int, Int2>,
    IDivisionOperators<Int2, int, Int2>,
    IUnaryPlusOperators<Int2, Int2>,
    IUnaryNegationOperators<Int2, Int2>
#endif
{
    /// <inheritdoc/>
    public static int Count => 2;

    /// <inheritdoc/>
    public static Int2 Zero => default;

    /// <inheritdoc/>
    public static Int2 One { get; } = new(1);

    /// <inheritdoc/>
    public static Int2 MaxValue { get; } = new(int.MaxValue);

    /// <inheritdoc/>
    public static Int2 MinValue { get; } = new(int.MinValue);

    private int _x;
    /// <inheritdoc/>
    public int X
    {
        readonly get => _x;
        set => _x = value;
    }

    private int _y;
    /// <inheritdoc/>
    public int Y
    {
        readonly get => _y;
        set => _y = value;
    }

    /// <inheritdoc/>
    public readonly int LengthSquared
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector64<int> vValue = AsVector64();
                return Vector64.Sum(vValue * vValue);
            }

#endif
            return X * X + Y * Y;
        }
    }

    /// <inheritdoc/>
    public readonly float Length => FastMath.SqrtEstimate(LengthSquared);

    /// <inheritdoc/>
    public readonly int Sum
    {
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                return Vector64.Sum(AsVector64());
            }

#endif
            return X + Y;
        }
    }

    /// <inheritdoc/>
    public int this[int index]
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
    /// <inheritdoc cref="Create(int)"/>
    public Int2(int value)
    {
        _x = value;
        _y = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Int2"/> structure with the specified x and y component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    public Int2(int x, int y)
    {
        _x = x;
        _y = y;
    }

    /// <inheritdoc/>
    public static Int2 Abs(in Int2 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = Vector64.Abs(vector.AsVector64());
            return FromVector64(vResult);
        }

#endif
        Unsafe.SkipInit(out Int2 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Int2 Clamp(in Int2 value, in Int2 min, in Int2 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = Vector64.Clamp(value.AsVector64(), min.AsVector64(), max.AsVector64());
            return FromVector64(vResult);
        }

#endif
        Unsafe.SkipInit(out Int2 sResult);
        sResult.X = int.Clamp(value.X, min.X, max.X);
        sResult.Y = int.Clamp(value.Y, min.Y, max.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Int2 Create(int value)
    {
        return new Int2(value);
    }

    /// <inheritdoc/>
    public static int Dot(in Int2 left, in Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.Dot(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X * right.X + left.Y * right.Y;
    }

    /// <inheritdoc/>
    public static Int2 Max(in Int2 left, in Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = Vector64.Max(left.AsVector64(), right.AsVector64());
            return FromVector64(vResult);
        }

#endif
        Unsafe.SkipInit(out Int2 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        return sResult;
    }

    /// <inheritdoc/>
    public static Int2 Min(in Int2 left, in Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = Vector64.Min(left.AsVector64(), right.AsVector64());
            return FromVector64(vResult);
        }

#endif
        Unsafe.SkipInit(out Int2 sResult);
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
    public static bool LessThanAll(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.LessThanAll(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X < right.X && left.Y < right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is less than or equal to
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Int2, Int2)"/>
    public static bool LessThanOrEqualAll(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAll(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X <= right.X && left.Y <= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Int2, Int2)"/>
    public static bool GreaterThanAll(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAll(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X > right.X && left.Y > right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if each component of <paramref name="left"/> is greater than or equal to 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Int2, Int2)"/>
    public static bool GreaterThanOrEqualAll(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAll(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X >= right.X && left.Y >= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is less than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAll(Int2, Int2)"/>
    public static bool LessThanAny(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.LessThanAny(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X < right.X || left.Y < right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is less than or equal to
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Int2, Int2)"/>
    public static bool LessThanOrEqualAny(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.LessThanOrEqualAny(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X <= right.X || left.Y <= right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is greater than 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Int2, Int2)"/>
    public static bool GreaterThanAny(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanAny(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X > right.X || left.Y > right.Y;
    }

    /// <returns>
    /// <see langword="true"/> if any component of <paramref name="left"/> is greater than or equal to 
    /// the corresponding component of <paramref name="right"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="LessThanAny(Int2, Int2)"/>
    public static bool GreaterThanOrEqualAny(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.GreaterThanOrEqualAny(left.AsVector64(), right.AsVector64());
        }

#endif
        return left.X >= right.X || left.Y >= right.Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int2 other)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.EqualsAll(AsVector64(), other.AsVector64());
        }

#endif
        return X == other.X && Y == other.Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Int2 other && Equals(other);
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
    private readonly Vector64<int> AsVector64() => Utils.ReinterpretCast<Int2, Vector64<int>>(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int2 FromVector64(Vector64<int> vector) => Utils.ReinterpretCast<Vector64<int>, Int2>(vector);
#endif

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 value)
    {
        return value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = left.AsVector64() + right.AsVector64();
            return FromVector64(vResult);
        }

#endif
        return new Int2(left.X + right.X, left.Y + right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 left, int right)
    {
        Int2 operand = new(right);
        return left + operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = -value.AsVector64();
            return FromVector64(vResult);
        }

#endif
        return new Int2(-value.X, -value.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = left.AsVector64() - right.AsVector64();
            return FromVector64(vResult);
        }

#endif
        return new Int2(left.X - right.X, left.Y - right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 left, int right)
    {
        Int2 operand = new(right);
        return left - operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = left.AsVector64() * right.AsVector64();
            return FromVector64(vResult);
        }

#endif
        return new Int2(left.X * right.X, left.Y * right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 left, int right)
    {
        Int2 operand = new(right);
        return left * operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator /(Int2 left, Int2 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<int> vResult = left.AsVector64() / right.AsVector64();
            return FromVector64(vResult);
        }

#endif
        return new Int2(left.X / right.X, left.Y / right.Y);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator /(Int2 left, int right)
    {
        Int2 operand = new(right);
        return left / operand;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int2 left, Int2 right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int2 left, Int2 right)
    {
        return !left.Equals(right);
    }

#if NET7_0_OR_GREATER
    static bool IVector<Int2, int>.ApproximatelyEquals(in Int2 left, in Int2 right)
    {
        return left.Equals(right);
    }
#endif
}
