namespace ProceduralGraph.Mathematics
{
    public partial struct Pixel128
    {
        /// <inheritdoc/>
        public static Pixel128 operator +(Pixel128 left, Pixel128 right)
        {
            return new Pixel128(left.Red + right.Red, left.Green + right.Green, left.Blue + right.Blue, left.Alpha + right.Alpha);
        }

        /// <inheritdoc/>
        public static Pixel128 operator +(Pixel128 left, float right)
        {
            return new Pixel128(left.Red + right, left.Green + right, left.Blue + right, left.Alpha + right);
        }

        /// <inheritdoc/>
        public static Pixel128 operator -(Pixel128 left, Pixel128 right)
        {
            return new Pixel128(left.Red - right.Red, left.Green - right.Green, left.Blue - right.Blue, left.Alpha - right.Alpha);
        }

        /// <inheritdoc/>
        public static Pixel128 operator -(Pixel128 left, float right)
        {
            return new Pixel128(left.Red - right, left.Green - right, left.Blue - right, left.Alpha - right);
        }

        /// <inheritdoc/>
        public static Pixel128 operator *(Pixel128 left, Pixel128 right)
        {
            return new Pixel128(left.Red * right.Red, left.Green * right.Green, left.Blue * right.Blue, left.Alpha * right.Alpha);
        }

        /// <inheritdoc/>
        public static Pixel128 operator *(Pixel128 left, float right)
        {
            return new Pixel128(left.Red * right, left.Green * right, left.Blue * right, left.Alpha * right);
        }

        /// <inheritdoc/>
        public static Pixel128 operator /(Pixel128 left, Pixel128 right)
        {
            return new Pixel128(left.Red / right.Red, left.Green / right.Green, left.Blue / right.Blue, left.Alpha / right.Alpha);
        }

        /// <inheritdoc/>
        public static Pixel128 operator /(Pixel128 left, float right)
        {
            return new Pixel128(left.Red / right, left.Green / right, left.Blue / right, left.Alpha / right);
        }
    }
}
