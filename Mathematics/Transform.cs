using System.Numerics;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Represents a 3D transformation consisting of scale, rotation, and translation components.
/// </summary>
/// <param name="Scale">The scaling factor applied to the transformation.</param>
/// <param name="Rotation">The rotation component represented as a quaternion.</param>
/// <param name="Translation">The translation component represented as a 3D vector.</param>
public readonly record struct Transform(Vector3 Scale, Quaternion Rotation, Double3 Translation);