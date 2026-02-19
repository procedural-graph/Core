using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics
{
    /// <summary>
    /// Represents a 32-bit vector containing red, green, blue, and alpha (RGBA) color channels, each stored as an 8-bit
    /// unsigned integer.
    /// </summary>
    public partial struct Pixel32 : IVector4<Pixel32, byte>
    {
        private const int ComponentCount = 4;

        /// <inheritdoc/>
        public static Pixel32 Zero => default;

        /// <inheritdoc/>
        public static Pixel32 One { get; } = Create(1);

        /// <inheritdoc/>
        public static Pixel32 MaxValue { get; } = Create(byte.MaxValue);

        /// <inheritdoc/>
        public static Pixel32 MinValue => default;

        private unsafe fixed byte _values[ComponentCount];

        /// <summary>
        /// Gets or sets the value of the red channel.
        /// </summary>
        public unsafe byte Red
        {
            readonly get => _values[0];
            set => _values[0] = value;
        }
        byte IVector4<Pixel32, byte>.X
        {
            readonly get => Red;
            set => Red = value;
        }

        /// <summary>
        /// Gets or sets the value of the green channel.
        /// </summary>
        public unsafe byte Green
        {
            readonly get => _values[1];
            set => _values[1] = value;
        }
        byte IVector4<Pixel32, byte>.Y
        {
            readonly get => Green;
            set => Green = value;
        }

        /// <summary>
        /// Gets or sets the value of the blue channel.
        /// </summary>
        public unsafe byte Blue
        {
            readonly get => _values[2];
            set => _values[2] = value;
        }
        byte IVector4<Pixel32, byte>.Z
        {
            readonly get => Blue;
            set => Blue = value;
        }

        /// <summary>
        /// Gets or sets the value of the alpha channel.
        /// </summary>
        public unsafe byte Alpha
        {
            readonly get => _values[3];
            set => _values[3] = value;
        }
        byte IVector4<Pixel32, byte>.W
        {
            readonly get => Alpha;
            set => Alpha = value;
        }

        /// <inheritdoc/>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than 3.</exception>
        public unsafe ref byte this[int index]
        {
            get
            {
                if ((uint)index >= ComponentCount)
                {
                    throw new IndexOutOfRangeException("Index must be in the range [0, 3].");
                }

                fixed (byte* ptr = _values)
                {
                    return ref ptr[index];
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixel32"/> structure with the specified red, green, blue and alpha values.
        /// </summary>
        /// <param name="red">The value to assign to the red channel.</param>
        /// <param name="green">The value to assign to the green channel.</param>
        /// <param name="blue">The value to assign to the blue channel.</param>
        /// <param name="alpha">The value to assign to the alpha channel.</param>
        public unsafe Pixel32(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
        {
            _values[0] = red;
            _values[1] = green;
            _values[2] = blue;
            _values[3] = alpha;
        }

        /// <summary>
        /// Deconstructs the instance into its red, green, blue, and alpha channel values.
        /// </summary>
        /// <param name="red">When this method returns, contains the value of the red channel.</param>
        /// <param name="green">When this method returns, contains the value of the green channel.</param>
        /// <param name="blue">When this method returns, contains the value of the blue channel.</param>
        /// <param name="alpha">When this method returns, contains the value of the alpha channel.</param>
        public readonly void Deconstruct(out byte red, out byte green, out byte blue, out byte alpha)
        {
            red = Red;
            green = Green; 
            blue = Blue; 
            alpha = Alpha;
        }

        /// <inheritdoc/>
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Pixel32 other && Equals(other);
        }

        /// <inheritdoc/>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            return $"<{Red.ToString(format, formatProvider)}{separator} {Green.ToString(format, formatProvider)}{separator} {Blue.ToString(format, formatProvider)}{separator} {Alpha.ToString(format, formatProvider)}>";
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly string ToString()
        {
            return ToString(null, null);
        }

        /// <inheritdoc/>
        public unsafe static Pixel32 Create(byte value)
        {
            Pixel32 result = default;
            byte* ptr = result._values;
            ptr[0] = value;
            ptr[1] = value;
            ptr[2] = value;
            ptr[3] = value;
            return result;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Pixel32 left, Pixel32 right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Pixel32 left, Pixel32 right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator +(Pixel32 left, Pixel32 right)
        {
            return ((Pixel128)left) + ((Pixel128)right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator +(Pixel32 left, float right)
        {
            return ((Pixel128)left) + right;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator -(Pixel32 left, Pixel32 right)
        {
            return ((Pixel128)left) - ((Pixel128)right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator -(Pixel32 left, float right)
        {
            return ((Pixel128)left) - right;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator *(Pixel32 left, Pixel32 right)
        {
            return ((Pixel128)left) * ((Pixel128)right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator *(Pixel32 left, float right)
        {
            return ((Pixel128)left) * right;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator /(Pixel32 left, Pixel32 right)
        {
            return ((Pixel128)left) / ((Pixel128)right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pixel128 operator /(Pixel32 left, float right)
        {
            return ((Pixel128)left) / right;
        }
    }
}
