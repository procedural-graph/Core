using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics;

public partial struct Pixel128 : IAdditionOperators<Pixel128, float, Pixel128>,
    ISubtractionOperators<Pixel128, float, Pixel128>,
    IMultiplyOperators<Pixel128, float, Pixel128>,
    IDivisionOperators<Pixel128, float, Pixel128>
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) + ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator +(Pixel128 left, float right)
    {
        return ((Vector4)left) + Vector4.Create(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) - ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator -(Pixel128 left, float right)
    {
        return ((Vector4)left) - Vector4.Create(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) * ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator *(Pixel128 left, float right)
    {
        return ((Vector4)left) * Vector4.Create(right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, Pixel128 right)
    {
        return ((Vector4)left) / ((Vector4)right);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel128 operator /(Pixel128 left, float right)
    {
        return ((Vector4)left) / Vector4.Create(right);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Pixel128"/> value to a <see cref="Vector4"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Pixel128"/> value to convert to a <see cref="Vector4"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4(Pixel128 value)
    {
        return Unsafe.BitCast<Pixel128, Vector4>(value);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Vector4"/> value to a <see cref="Pixel128"/> instance.
    /// </summary>
    /// <param name="value">The <see cref="Vector4"/> value to convert to a <see cref="Pixel128"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel128(Vector4 value)
    {
        return Unsafe.BitCast<Vector4, Pixel128>(value);
    }
}
