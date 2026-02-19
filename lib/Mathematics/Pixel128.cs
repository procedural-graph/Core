using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics
{
    /// <summary>
    /// Represents a 128-bit vector containing red, green, blue, and alpha (RGBA) color channels, each stored as an 32-bit
    /// floating point number.
    /// </summary>
    public partial struct Pixel128 : IVector4<Pixel128, float>
    {
        private const int ComponentCount = 4;

        /// <inheritdoc/>
        public static Pixel128 Zero => default;

        /// <inheritdoc/>
        public static Pixel128 One { get; } = Create(1.0f);

        /// <inheritdoc/>
        public static Pixel128 MaxValue { get; } = Create(float.MaxValue);

        /// <inheritdoc/>
        public static Pixel128 MinValue { get; } = Create(float.MinValue);

        private unsafe fixed float _values[ComponentCount];

        /// <summary>
        /// Gets or sets the value of the red channel.
        /// </summary>
        public unsafe float Red
        {
            readonly get => _values[0];
            set => _values[0] = value;
        }
        float IVector4<Pixel128, float>.X
        {
            readonly get => Red;
            set => Red = value;
        }

        /// <summary>
        /// Gets or sets the value of the green channel.
        /// </summary>
        public unsafe float Green
        {
            readonly get => _values[1];
            set => _values[1] = value;
        }
        float IVector4<Pixel128, float>.Y
        {
            readonly get => Green;
            set => Green = value;
        }

        /// <summary>
        /// Gets or sets the value of the blue channel.
        /// </summary>
        public unsafe float Blue
        {
            readonly get => _values[2];
            set => _values[2] = value;
        }
        float IVector4<Pixel128, float>.Z
        {
            readonly get => Blue;
            set => Blue = value;
        }

        /// <summary>
        /// Gets or sets the value of the alpha channel.
        /// </summary>
        public unsafe float Alpha
        {
            readonly get => _values[3];
            set => _values[3] = value;
        }
        float IVector4<Pixel128, float>.W
        {
            readonly get => Alpha;
            set => Alpha = value;
        }

        /// <inheritdoc/>
        /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than 3.</exception>
        public unsafe ref float this[int index]
        {
            get
            {
                if ((uint)index >= ComponentCount)
                {
                    throw new IndexOutOfRangeException("Index must be in the range [0, 3].");
                }

                fixed (float* ptr = _values)
                {
                    return ref ptr[index];
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixel128"/> structure with the specified red, green, blue and alpha values.
        /// </summary>
        /// <param name="red">The value to assign to the red channel.</param>
        /// <param name="green">The value to assign to the green channel.</param>
        /// <param name="blue">The value to assign to the blue channel.</param>
        /// <param name="alpha">The value to assign to the alpha channel.</param>
        public unsafe Pixel128(float red, float green, float blue, float alpha)
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
        public readonly void Deconstruct(out float red, out float green, out float blue, out float alpha)
        {
            red = Red;
            green = Green; 
            blue = Blue; 
            alpha = Alpha;
        }

        /// <inheritdoc/>
        public readonly bool Equals(Pixel128 other)
        {
            return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
        }

        /// <inheritdoc/>
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Pixel128 other && Equals(other);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Red, Green, Blue, Alpha);
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
        public static unsafe Pixel128 Create(float value)
        {
            Pixel128 result = default;
            float* ptr = result._values;
            ptr[0] = value;
            ptr[1] = value;
            ptr[2] = value;
            ptr[3] = value;
            return result;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Pixel128 left, Pixel128 right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Pixel128 left, Pixel128 right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Converts an <see cref="Pixel32"/> to an <see cref="Pixel128"/> by normalizing each channel to the range
        /// 0.0 to 1.0.
        /// </summary>
        /// <param name="value">The 32-bit RGBA color value to convert.</param>
        public static implicit operator Pixel128(Pixel32 value)
        {
            const float scale = 1.0f / 255.0f;
            return new Pixel128(value.Red * scale, value.Green * scale, value.Blue * scale, value.Alpha * scale);
        }

        /// <summary>
        /// Converts an <see cref="Pixel128"/> to an <see cref="Pixel32"/> by mapping each channel from floating-point to byte
        /// precision.
        /// </summary>
        /// <param name="value">The <see cref="Pixel128"/> instance to convert. Each channel should be in the range 0.0 to 1.0.</param>
        public static explicit operator Pixel32(Pixel128 value)
        {
            float maxValue = byte.MaxValue;
            return new Pixel32((byte)(value.Red * maxValue), (byte)(value.Green * maxValue), (byte)(value.Blue * maxValue), (byte)(value.Alpha * maxValue));
        }
    }
}
