using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a 4x4 matrix.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Matrix : IEquatable<Matrix>
{
    /// <summary>Gets or sets the value at row 1, column 1 of the matrix.</summary>
    public double M11 { readonly get; set; }
    /// <summary>Gets or sets the value at row 1, column 2 of the matrix.</summary>
    public double M12 { readonly get; set; }
    /// <summary>Gets or sets the value at row 1, column 3 of the matrix.</summary>
    public double M13 { readonly get; set; }
    /// <summary>Gets or sets the value at row 1, column 4 of the matrix.</summary>
    public double M14 { readonly get; set; }

    /// <summary>Gets or sets the value at row 2, column 1 of the matrix.</summary>
    public double M21 { readonly get; set; }
    /// <summary>Gets or sets the value at row 2, column 2 of the matrix.</summary>
    public double M22 { readonly get; set; }
    /// <summary>Gets or sets the value at row 2, column 3 of the matrix.</summary>
    public double M23 { readonly get; set; }
    /// <summary>Gets or sets the value at row 2, column 4 of the matrix.</summary>
    public double M24 { readonly get; set; }

    /// <summary>Gets or sets the value at row 3, column 1 of the matrix.</summary>
    public double M31 { readonly get; set; }
    /// <summary>Gets or sets the value at row 3, column 2 of the matrix.</summary>
    public double M32 { readonly get; set; }
    /// <summary>Gets or sets the value at row 3, column 3 of the matrix.</summary>
    public double M33 { readonly get; set; }
    /// <summary>Gets or sets the value at row 3, column 4 of the matrix.</summary>
    public double M34 { readonly get; set; }

    /// <summary>Gets or sets the value at row 4, column 1 of the matrix.</summary>
    public double M41 { readonly get; set; }
    /// <summary>Gets or sets the value at row 4, column 2 of the matrix.</summary>
    public double M42 { readonly get; set; }
    /// <summary>Gets or sets the value at row 4, column 3 of the matrix.</summary>
    public double M43 { readonly get; set; }
    /// <summary>Gets or sets the value at row 4, column 4 of the matrix.</summary>
    public double M44 { readonly get; set; }

    /// <summary>
    /// Gets or sets the first row of the matrix.
    /// </summary>
    public Double4 X
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            VectorMath.GetComponent(in this, 0, out Double4 result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => VectorMath.SetComponent(ref this, 0, value);
    }

    /// <summary>
    /// Gets or sets the second row of the matrix.
    /// </summary>
    public Double4 Y
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            VectorMath.GetComponent(in this, 1, out Double4 result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => VectorMath.SetComponent(ref this, 1, value);
    }

    /// <summary>
    /// Gets or sets the third row of the matrix.
    /// </summary>
    public Double4 Z
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            VectorMath.GetComponent(in this, 2, out Double4 result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => VectorMath.SetComponent(ref this, 2, value);
    }

    /// <summary>
    /// Gets or sets the fourth row of the matrix.
    /// </summary>
    public Double4 W
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            VectorMath.GetComponent(in this, 3, out Double4 result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => VectorMath.SetComponent(ref this, 3, value);
    }

    /// <inheritdoc/>
    public readonly bool Equals(Matrix other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Matrix other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(M11);
        hashCode.Add(M12);
        hashCode.Add(M13);
        hashCode.Add(M14);
        hashCode.Add(M21);
        hashCode.Add(M22);
        hashCode.Add(M23);
        hashCode.Add(M24);
        hashCode.Add(M31);
        hashCode.Add(M32);
        hashCode.Add(M33);
        hashCode.Add(M34);
        hashCode.Add(M41);
        hashCode.Add(M42);
        hashCode.Add(M43);
        hashCode.Add(M44);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Compares two values to determine equality.
    /// </summary>
    /// <param name="left">The value to compare with <paramref name="right"/>.</param>
    /// <param name="right">The value to compare with <paramref name="left"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is equal to <paramref name="right"/>; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Matrix left, Matrix right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two values to determine inequality.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> is not equal to <paramref name="right"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="operator ==(Matrix, Matrix)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Matrix left, Matrix right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Matrix4x4"/> to a <see cref="Matrix"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Matrix(in Matrix4x4 value)
    {
        Unsafe.SkipInit(out Matrix result);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            ref Vector256<double> destinationRef = ref Unsafe.As<Matrix, Vector256<double>>(ref result);
            ref Vector128<float> sourceRef = ref Unsafe.As<Matrix4x4, Vector128<float>>(ref Unsafe.AsRef(in value));
            for (int i = 0; i < 4; i++)
            {
                Vector256<float> currentRow = Utils.BitCastWrite<Vector128<float>, Vector256<float>>(Unsafe.Add(ref sourceRef, i));
                Unsafe.Add(ref destinationRef, i) = Vector256.WidenLower(currentRow);
            }
            return result;
        }

#endif
        result.M11 = value.M11;
        result.M12 = value.M12;
        result.M13 = value.M13;
        result.M14 = value.M14;
        result.M21 = value.M21;
        result.M22 = value.M22;
        result.M23 = value.M23;
        result.M24 = value.M24;
        result.M31 = value.M31;
        result.M32 = value.M32;
        result.M33 = value.M33;
        result.M34 = value.M34;
        result.M41 = value.M41;
        result.M42 = value.M42;
        result.M43 = value.M43;
        result.M44 = value.M44;

        return result;
    }

    /// <summary>
    /// Explicitly converts a <see cref="Matrix"/> to a <see cref="Matrix4x4"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static explicit operator Matrix4x4(in Matrix value)
    {
        Unsafe.SkipInit(out Matrix4x4 result);

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            ref Vector256<double> sourceRef = ref Unsafe.As<Matrix, Vector256<double>>(ref Unsafe.AsRef(in value));
            ref Vector128<float> destinationRef = ref Unsafe.As<Matrix4x4, Vector128<float>>(ref result);
            for (int i = 0; i < 4; i++)
            {
                Vector256<double> currentRow = Unsafe.Add(ref sourceRef, i);
                Unsafe.Add(ref destinationRef, i) = Vector128.Narrow(currentRow.GetLower(), currentRow.GetUpper());
            }
            return result;
        }

#endif
        result.M11 = (float)value.M11;
        result.M12 = (float)value.M12;
        result.M13 = (float)value.M13;
        result.M14 = (float)value.M14;
        result.M21 = (float)value.M21;
        result.M22 = (float)value.M22;
        result.M23 = (float)value.M23;
        result.M24 = (float)value.M24;
        result.M31 = (float)value.M31;
        result.M32 = (float)value.M32;
        result.M33 = (float)value.M33;
        result.M34 = (float)value.M34;
        result.M41 = (float)value.M41;
        result.M42 = (float)value.M42;
        result.M43 = (float)value.M43;
        result.M44 = (float)value.M44;

        return result;
    }
}
