using System;

namespace GameSharp.ProceduralGraph.Mathematics;

/// <summary>
/// Defines a two-dimensional vector with <typeparamref name="TValue"/> X and Y components.
/// </summary>
/// <inheritdoc/>
public interface IVector2<TSelf, TValue> : IVector<TSelf, TValue>
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
    /// Deconstructs the current instance into it's X and Y component values.
    /// </summary>
    /// <param name="x">When this method returns, contains the value of the X component.</param>
    /// <param name="y">When this method returns, contains the value of the Y component.</param>
    void Deconstruct(out TValue x, out TValue y);
}
