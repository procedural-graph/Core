using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace ProceduralGraph.Mathematics;

[StructLayout(LayoutKind.Sequential)]
internal struct Int4 : IVector4<Int4, short>
#if NET7_0_OR_GREATER
    , IAdditionOperators<Int4, short, Int4>,
    ISubtractionOperators<Int4, short, Int4>,
    IMultiplyOperators<Int4, short, Int4>,
    IDivisionOperators<Int4, short, Int4>,
    IUnaryPlusOperators<Int4, Int4>,
    IUnaryNegationOperators<Int4, Int4>
#endif
{
    public static int Count => 4;

    public static Int4 Zero { get; } = Create(0);

    public static Int4 One { get; } = Create(1);

    public static Int4 MaxValue { get; } = Create(short.MaxValue);

    public static Int4 MinValue { get; } = Create(short.MinValue);

    public short X { readonly get; set; }

    public short Y { readonly get; set; }

    public short Z { readonly get; set; }

    public short W { readonly get; set; }

    public readonly short LengthSquared
    {
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                Vector64<short> vValue = AsVector64();
                return Vector64.Sum(vValue * vValue);
            }
#endif
            return Clamp(X * X + Y * Y + Z * Z + W * W);
        }
    }

    /// <inheritdoc/>
    public readonly float Length => FastMath.SqrtEstimate(LengthSquared);

    public readonly short Sum
    {
        get
        {
#if NETCOREAPP3_0_OR_GREATER
            if (Vector.IsHardwareAccelerated)
            {
                return Vector64.Sum(AsVector64());
            }

#endif
            return Clamp(X + Y + Z + W);
        }
    }

    /// <inheritdoc/>
    public short this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            ThrowHelpers.ThrowIfOutOfRange(index, Count);
            VectorMath.GetComponent(in this, index, out short result);
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
    /// Initializes a new instance of the <see cref="Int4"/> structure with the specified x, y, and z component values.
    /// </summary>
    /// <param name="x">The value to assign to the x component.</param>
    /// <param name="y">The value to assign to the y component.</param>
    /// <param name="z">The value to assign to the z component.</param>
    /// <param name="w">The value to assign to the w component.</param>
    public Int4(short x, short y, short z, short w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Creates a new <see cref="Int4"/> whose components all have the same value.
    /// </summary>
    /// <returns/>
    /// <inheritdoc cref="Create(short)"/>
    public Int4(short value)
    {
        X = value;
        Y = value;
        Z = value;
        W = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 Create(short value)
    {
        return new Int4(value);
    }

    /// <inheritdoc/>
    public static short Dot(in Int4 left, in Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.Dot(left.AsVector64(), right.AsVector64());
        }
#endif
        return Clamp(left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W);
    }

    /// <inheritdoc/>
    public static Int4 Max(in Int4 left, in Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = Vector64.Max(left.AsVector64(), right.AsVector64());
            return FromVector64(vResult);
        }
#endif
        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Math.Max(left.X, right.X);
        sResult.Y = Math.Max(left.Y, right.Y);
        sResult.Z = Math.Max(left.Z, right.Z);
        sResult.W = Math.Max(left.W, right.W);
        return sResult;
    }

    /// <inheritdoc/>
    public static Int4 Min(in Int4 left, in Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = Vector64.Min(left.AsVector64(), right.AsVector64());
            return Utils.ReinterpretCast<Vector64<short>, Int4>(vResult);
        }
#endif
        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Math.Min(left.X, right.X);
        sResult.Y = Math.Min(left.Y, right.Y);
        sResult.Z = Math.Min(left.Z, right.Z);
        sResult.W = Math.Min(left.W, right.W);
        return sResult;
    }

    /// <inheritdoc/>
    public readonly void Deconstruct(out short x, out short y, out short z, out short w)
    {
        x = X;
        y = Y;
        z = Z;
        w = W;
    }

    /// <inheritdoc/>
    public readonly bool Equals(Int4 other)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            return Vector64.EqualsAll(AsVector64(), other.AsVector64());
        }
#endif
        return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}{separator} {W.ToString(format, formatProvider)}>";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString()
    {
        return ToString(null, CultureInfo.CurrentCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Int4 other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    public static Int4 Clamp(in Int4 value, in Int4 min, in Int4 max)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = Vector64.Clamp(value.AsVector64(), min.AsVector64(), max.AsVector64());
            return FromVector64(vResult);
        }
#endif
        Int4 sResult = value;
        sResult.X = Clamp(value.X, min.X, max.X);
        sResult.Y = Clamp(value.Y, min.Y, max.Y);
        sResult.Z = Clamp(value.Z, min.Z, max.Z);
        sResult.W = Clamp(value.W, min.W, max.W);
        return sResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static short Clamp(int value, short min = short.MinValue, short max = short.MaxValue)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return (short)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 Abs(in Int4 vector)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = Vector64.Abs(vector.AsVector64());
            return FromVector64(vResult);
        }

#endif
        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Math.Abs(vector.X);
        sResult.Y = Math.Abs(vector.Y);
        sResult.Z = Math.Abs(vector.Z);
        sResult.W = Math.Abs(vector.W);
        return sResult;
    }

#if NETCOREAPP3_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Vector64<short> AsVector64()
    {
        return Utils.ReinterpretCast<Int4, Vector64<short>>(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Int4 FromVector64(Vector64<short> value)
    {
        return Utils.ReinterpretCast<Vector64<short>, Int4>(value);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int4 left, Int4 right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int4 left, Int4 right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 operator +(Int4 value)
    {
        return value;
    }

    public static Int4 operator +(Int4 left, Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = left.AsVector64() + right.AsVector64();
            return Utils.ReinterpretCast<Vector64<short>, Int4>(vResult);
        }
#endif

        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Clamp(left.X + right.X);
        sResult.Y = Clamp(left.Y + right.Y);
        sResult.Z = Clamp(left.Z + right.Z);
        sResult.W = Clamp(left.W + right.W);
        return sResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 operator +(Int4 left, short right)
    {
        return left + Create(right);
    }

    public static Int4 operator -(Int4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = -value.AsVector64();
            return FromVector64(vResult);
        }
#endif

        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Clamp(-value.X);
        sResult.Y = Clamp(-value.Y);
        sResult.Z = Clamp(-value.Z);
        sResult.W = Clamp(-value.W);
        return sResult;
    }

    public static Int4 operator -(Int4 left, Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = left.AsVector64() - right.AsVector64();
            return FromVector64(vResult);
        }
#endif

        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Clamp(left.X - right.X);
        sResult.Y = Clamp(left.Y - right.Y);
        sResult.Z = Clamp(left.Z - right.Z);
        sResult.W = Clamp(left.W - right.W);
        return sResult;
    }

    public static Int4 operator -(Int4 left, short right)
    {
        return left - Create(right);
    }

    public static Int4 operator *(Int4 left, Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = left.AsVector64() * right.AsVector64();
            return FromVector64(vResult);
        }
#endif

        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Clamp(left.X * right.X);
        sResult.Y = Clamp(left.Y * right.Y);
        sResult.Z = Clamp(left.Z * right.Z);
        sResult.W = Clamp(left.W * right.W);
        return sResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 operator *(Int4 left, short right)
    {
        return left * Create(right);
    }

    public static Int4 operator /(Int4 left, Int4 right)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vResult = left.AsVector64() / right.AsVector64();
            return FromVector64(vResult);
        }
#endif

        Unsafe.SkipInit(out Int4 sResult);
        sResult.X = Clamp(left.X / right.X);
        sResult.Y = Clamp(left.Y / right.Y);
        sResult.Z = Clamp(left.Z / right.Z);
        sResult.W = Clamp(left.W / right.W);
        return sResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int4 operator /(Int4 left, short right)
    {
        return left / Create(right);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pixel32(Int4 value)
    {
        Int4 min = Create(byte.MinValue);
        Int4 max = Create(byte.MaxValue);
        Int4 clamped = Clamp(in value, min, max);
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<short> vClamped = clamped.AsVector64();
            Vector64<byte> vBytes = Vector64.Narrow(vClamped.AsUInt16(), Vector64<ushort>.Zero);
            return Unsafe.As<Vector64<byte>, Pixel32>(ref vBytes);
        }
#endif
        return new Pixel32((byte)clamped.X, (byte)clamped.Y, (byte)clamped.Z, (byte)clamped.W);
    }

    public static explicit operator Int4(Pixel32 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector64<byte> vBytes = Utils.BitCastWrite<Pixel32, Vector64<byte>>(value);
            Vector64<ushort> vShorts = Vector64.WidenLower(vBytes);
            return FromVector64(vShorts.AsInt16());
        }
#endif
        return new Int4(value.Red, value.Green, value.Blue, value.Alpha);
    }

    public static explicit operator Pixel128(Int4 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<short> vShorts = Utils.BitCastWrite<Int4, Vector128<short>>(value);
            Vector128<int> vIntegers = Vector128.WidenLower(vShorts);
            Vector128<float> vFloats = Vector128.ConvertToSingle(vIntegers);
            return Utils.ReinterpretCast<Vector128<float>, Pixel128>(vFloats);
        }

#endif
        return new Pixel128(value.X, value.Y, value.Z, value.W);
    }

    public static explicit operator Int4(Pixel128 value)
    {
#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector128<float> vFloats = Utils.ReinterpretCast<Pixel128, Vector128<float>>(value);
            Vector128<int> vIntegers = Vector128.ConvertToInt32(vFloats);
            Vector64<short> vShorts = Vector64.Narrow(vIntegers.GetLower(), vIntegers.GetUpper());
            return FromVector64(vShorts);
        }

#endif
        return new Int4((short)value.Red, (short)value.Green, (short)value.Blue, (short)value.Alpha);
    }

#if NET7_0_OR_GREATER
    static bool IVector<Int4, short>.ApproximatelyEquals(in Int4 left, in Int4 right)
    {
        return left.Equals(right);
    }
#endif
}
