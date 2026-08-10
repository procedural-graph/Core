using System.Numerics;

namespace GameSharp.ProceduralGraph.Mathematics;

/// <summary>
/// Represents a 3D transformation consisting of scale, rotation, and translation components.
/// </summary>
/// <param name="Scale">The scaling factor applied to the transformation.</param>
/// <param name="Rotation">The rotation component represented as a quaternion.</param>
/// <param name="Translation">The translation component represented as a 3D vector.</param>
public readonly record struct Transform(Vector3 Scale, Quaternion Rotation, Double3 Translation)
{
    /// <summary>
    /// Gets the identity transformation, which has a scale of (1, 1, 1), no rotation, and no translation.
    /// </summary>
    public static Transform Identity { get; } = new Transform(Vector3.One, Quaternion.Identity, Double3.Zero);

    /// <summary>
    /// Transforms the specified point from local space to world space.
    /// </summary>
    /// <param name="point">The point to transform, represented as a <see cref="Double3"/>.</param>
    /// <returns>A <see cref="Double3"/> representing the point in world space.</returns>
    public Double3 TransformPoint(Double3 point) => Double3.Transform(point * Scale, Rotation) + Translation;

    /// <summary>
    /// Transforms the specified point from world space to local space.
    /// </summary>
    /// <param name="point">The point in world space to be transformed into the object's local coordinate system.</param>
    /// <returns>A <see cref="Double3"/> representing the point in local space.</returns>
    public Double3 InverseTransformPoint(Double3 point) => Double3.Transform(point - Translation, Quaternion.Inverse(Rotation)) / Scale;
}