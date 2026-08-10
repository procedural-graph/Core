using System;

namespace GameSharp.ProceduralGraph.Mathematics;

/// <summary>
/// Defines a four-dimensional vector with <typeparamref name="TValue"/> components.
/// </summary>
/// <inheritdoc/>
public interface IVector4<TSelf, TValue> : IVector<TSelf, TValue>
    where TValue : struct, IEquatable<TValue>
    where TSelf : IVector<TSelf, TValue>
{
    /// <summary>
    /// Gets or sets the value of the X component.
    /// </summary>
    TValue X { get; set; }

    /// <summary>
    /// Gets or sets the value of the Y component.
    /// </summary>
    TValue Y { get; set; }

    /// <summary>
    /// Gets or sets the value of the Z component.
    /// </summary>
    TValue Z { get; set; }

    /// <summary>
    /// Gets or sets the value of the W component.
    /// </summary>
    TValue W { get; set; }

    /// <summary>
    /// Deconstructs the current instance into its X, Y, Z and W component values.
    /// </summary>
    /// <param name="x">When this method returns, contains the value of the X component.</param>
    /// <param name="y">When this method returns, contains the value of the Y component.</param>
    /// <param name="z">When this method returns, contains the value of the Z component.</param>
    /// <param name="w">When this method returns, contains the value of the W component.</param>
    void Deconstruct(out TValue x, out TValue y, out TValue z, out TValue w);
}
