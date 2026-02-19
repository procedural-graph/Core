using System;

namespace ProceduralGraph.Mathematics
{
    public partial struct Pixel32
    {
        /// <inheritdoc/>
        public readonly bool Equals(Pixel32 other)
        {
            return Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Red, Green, Blue, Alpha);
        }
    }
}
