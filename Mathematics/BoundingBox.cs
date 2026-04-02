using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents an Axis-Aligned Bounding Box (AABB) in 3D space using double-precision coordinates.
/// </summary>
public readonly record struct BoundingBox
{
    /// <summary>
    /// The minimum extents of the bounding box.
    /// </summary>
    public Double3 Min { get; init; }

    /// <summary>
    /// The maximum extents of the bounding box.
    /// </summary>
    public Double3 Max { get; init; }

    /// <summary>
    /// Gets the size of the bounding box as a three-dimensional vector representing the difference between the maximum
    /// and minimum coordinates in each dimension.
    /// </summary>
    public Double3 Size => Max - Min;

    /// <summary>
    /// Gets the center point of the bounding box, calculated as the midpoint between the minimum and maximum
    /// coordinates.
    /// </summary>
    public Double3 Center => (Min + Max) * 0.5;

    /// <summary>
    /// Represents an empty or uninitialized bounding box. 
    /// Merging any valid box with this will result in the valid box.
    /// </summary>
    public static BoundingBox Empty { get; } = new BoundingBox(Double3.Create(double.PositiveInfinity), Double3.Create(double.NegativeInfinity));

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundingBox"/> structure using the specified minimum and maximum corner points.
    /// </summary>
    /// <param name="min">
    /// The minimum corner point of the bounding box, represented as a <see cref="Vector3"/> structure. 
    /// Each component must be less
    /// than or equal to the corresponding component of <paramref name="max"/>.
    /// </param>
    /// <param name="max">
    /// The maximum corner point of the bounding box, represented as a <see cref="Vector3"/> structure. 
    /// Each component must be greater than or equal to the corresponding component of <paramref name="min"/>.
    /// </param>
    public BoundingBox(Double3 min, Double3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Determines whether this bounding box overlaps with the specified bounding box in three-dimensional space.
    /// </summary>
    /// <param name="other">The bounding box to test for intersection with this bounding box.</param>
    /// <returns><see langword="true"/> if the bounding boxes intersect; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingBox other)
    {
        return Double3.LessThanOrEqualAll(Min, other.Max) && Double3.GreaterThanOrEqualAll(Max, other.Min);
    }

    /// <summary>
    /// Creates a bounding box that encapsulates a single Transform.
    /// Assumes the transform represents a 1x1x1 unit cube centered at its origin.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox CreateFromTransform(Transform transform)
    {
        return CreateFromTransform(transform, Double3.One);
    }

    /// <summary>
    /// Creates a bounding box that encapsulates a single Transform with a specified size.
    /// </summary>
    /// <param name="transform">The transform to create the bounding box from.</param>
    /// <param name="size">The size of the box in local space before transformation.</param>
    public static unsafe BoundingBox CreateFromTransform(Transform transform, Double3 size)
    {
        const int CornerCount = 8;

        Double3 half = size * 0.5;

        Double3* corners = stackalloc Double3[CornerCount]
        {
            new(-half.X, -half.Y, -half.Z), new(+half.X, -half.Y, -half.Z),
            new(-half.X, +half.Y, -half.Z), new(+half.X, +half.Y, -half.Z),
            new(-half.X, -half.Y, +half.Z), new(+half.X, -half.Y, +half.Z),
            new(-half.X, +half.Y, +half.Z), new(+half.X, +half.Y, +half.Z)
        };

        Double3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);

        for (int i = 0; i < CornerCount; i++)
        {
            Double3 scaled = corners[i] * transform.Scale;
            Double3 rotated = Double3.Transform(scaled, transform.Rotation);
            Double3 world = rotated + transform.Translation;
            min = Double3.Min(min, world);
            max = Double3.Max(max, world);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Merges this bounding box with another, returning a new bounding box that encapsulates both.
    /// </summary>
    public BoundingBox Merge(BoundingBox other) => new(Double3.Min(Min, other.Min), Double3.Max(Max, other.Max));
}