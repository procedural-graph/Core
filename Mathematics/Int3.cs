using System;
using System.Globalization;

namespace ProceduralGraph.Mathematics
{
    /// <summary>
    /// Represents a three-dimensional vector with integer X, Y, and Z components.
    /// </summary>
    public unsafe struct Int3 : IVector3<Int3, int>
#if NET7_0_OR_GREATER
        , System.Numerics.IAdditionOperators<Int3, int, Int3>,
        System.Numerics.ISubtractionOperators<Int3, int, Int3>,
        System.Numerics.IMultiplyOperators<Int3, int, Int3>,
        System.Numerics.IDivisionOperators<Int3, int, Int3>
#endif
    {
        private const int ComponentCount = 3;

        /// <inheritdoc/>
        public static Int3 Zero => default;

        /// <inheritdoc/>
        public static Int3 One { get; } = new Int3(1, 1, 1);

        /// <inheritdoc/>
        public static Int3 MaxValue { get; } = new Int3(int.MaxValue, int.MaxValue, int.MaxValue);

        /// <inheritdoc/>
        public static Int3 MinValue { get; } = new Int3(int.MinValue, int.MinValue, int.MinValue);

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Int3"/> structure with the specified x, y, and z component values.
        /// </summary>
        /// <param name="x">The value to assign to the x component.</param>
        /// <param name="y">The value to assign to the y component.</param>
        /// <param name="z">The value to assign to the z component.</param>
        public Int3(int x, int y, int z)
        {
            _values[0] = x;
            _values[1] = y;
            _values[2] = z;
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

        /// <inheritdoc/>
        public int Z
        {
            readonly get => _values[2];
            set => _values[2] = value;
        }

        /// <inheritdoc/>
        public readonly bool Equals(Int3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object? obj)
        {
            return obj is Int3 other && Equals(other);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

        /// <inheritdoc/>
        public readonly void Deconstruct(out int x, out int y, out int z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider = null)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
        }

        /// <inheritdoc/>
        public override readonly string ToString() => ToString(null, null);

        /// <inheritdoc/>
        public static Int3 Create(int value)
        {
            Int3 result = default;
            int* ptr = result._values;
            ptr[0] = value;
            ptr[1] = value;
            ptr[2] = value;
            return result;
        }

        /// <inheritdoc/>
        public static bool operator ==(Int3 left, Int3 right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Int3 left, Int3 right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public static Int3 operator +(Int3 left, Int3 right)
        {
            return new Int3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        /// <inheritdoc/>
        public static Int3 operator +(Int3 left, int right)
        {
            return new Int3(left.X + right, left.Y + right, left.Z + right);
        }

        /// <inheritdoc/>
        public static Int3 operator -(Int3 left, Int3 right)
        {
            return new Int3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        /// <inheritdoc/>
        public static Int3 operator -(Int3 left, int right)
        {
            return new Int3(left.X - right, left.Y - right, left.Z - right);
        }

        /// <inheritdoc/>
        public static Int3 operator *(Int3 left, Int3 right)
        {
            return new Int3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        /// <inheritdoc/>
        public static Int3 operator *(Int3 left, int right)
        {
            return new Int3(left.X * right, left.Y * right, left.Z * right);
        }

        /// <inheritdoc/>
        public static Int3 operator /(Int3 left, Int3 right)
        {
            return new Int3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
        }

        /// <inheritdoc/>
        public static Int3 operator /(Int3 left, int right)
        {
            return new Int3(left.X / right, left.Y / right, left.Z / right);
        }
    }
}
