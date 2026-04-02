using System;

namespace ProceduralGraph.Mathematics;

/// <summary>
/// Defines a generic interface for mathematical vectors that support component-wise operations, formatting, and
/// equality comparison.
/// </summary>
/// <typeparam name="TSelf">The type that implements the vector interface.</typeparam>
/// <typeparam name="TValue">The type of the vector's components.</typeparam>
public interface IVector<TSelf, TValue> : IEquatable<TSelf>, IFormattable 
#if NET7_0_OR_GREATER
    , System.Numerics.IMinMaxValue<TSelf>,
    System.Numerics.IAdditionOperators<TSelf, TSelf, TSelf>, 
    System.Numerics.IDivisionOperators<TSelf, TSelf, TSelf>, 
    System.Numerics.IEqualityOperators<TSelf, TSelf, bool>, 
    System.Numerics.IMultiplyOperators<TSelf, TSelf, TSelf>, 
    System.Numerics.ISubtractionOperators<TSelf, TSelf, TSelf>
#endif
    where TValue : struct, IEquatable<TValue> 
    where TSelf : IVector<TSelf, TValue>
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// Gets the number of components in the vector.
    /// </summary>
    static abstract int Count { get; }

    /// <summary>
    /// Gets a vector whose components are all zero.
    /// </summary>
    static abstract TSelf Zero { get; }

    /// <summary>
    /// Gets a vector whose components are all set to one.
    /// </summary>
    static abstract TSelf One { get; }

    /// <summary>
    /// Creates a new <typeparamref name="TSelf"/> whose components all have the same value.
    /// </summary>
    /// <param name="value">The value to assign to all components.</param>
    /// <returns>A new <typeparamref name="TSelf"/> whose components all have the same value.</returns>
    static abstract TSelf Create(TValue value);

    /// <summary>
    /// Computes the absolute value of each element in a vector.
    /// </summary>
    /// <param name="vector">The vector that will have its absolute value computed.</param>
    /// <returns>A vector whose elements are the absolute value of the elements in <paramref name="vector"/>.</returns>
    static abstract TSelf Abs(in TSelf vector);

    /// <summary>
    /// Returns a new <typeparamref name="TSelf"/> whose components are the minimum values of the corresponding components of two specified
    /// <typeparamref name="TSelf"/> instances.
    /// </summary>
    /// <param name="left">The first <typeparamref name="TSelf"/> instance to compare.</param>
    /// <param name="right">The second <typeparamref name="TSelf"/> instance to compare.</param>
    /// <returns>
    /// A <typeparamref name="TSelf"/> whose components are the minimum values from the corresponding components of the 
    /// <paramref name="left"/> and <paramref name="right"/> parameters.
    /// </returns>
    static abstract TSelf Min(in TSelf left, in TSelf right);

    /// <summary>
    /// Returns a new <typeparamref name="TSelf"/> whose components are the maximum values of the corresponding components of two specified
    /// <typeparamref name="TSelf"/> instances.
    /// </summary>
    /// <returns>
    /// A <typeparamref name="TSelf"/> whose components are the maximum values from the corresponding components of the 
    /// <paramref name="left"/> and <paramref name="right"/> parameters.
    /// </returns>
    /// <inheritdoc cref="Min(in TSelf, in TSelf)"/>
    static abstract TSelf Max(in TSelf left, in TSelf right);

    /// <summary>
    /// Returns the dot product of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product.</returns>
    static abstract TValue Dot(in TSelf left, in TSelf right);

    /// <summary>
    /// Restricts a vector to be within the specified minimum and maximum bounds.
    /// </summary>
    /// <param name="value">The value to be clamped within the specified range.</param>
    /// <param name="min">The inclusive lower bound to which the value will be clamped.</param>
    /// <param name="max">The inclusive upper bound to which the value will be clamped.</param>
    /// <returns>The value constrained to be no less than the specified minimum and no greater than the specified maximum.</returns>
    static abstract TSelf Clamp(in TSelf value, in TSelf min, in TSelf max);
#endif

    /// <summary>
    /// Gets the vector's length squared.
    /// </summary>
    TValue LengthSquared { get; }

    /// <summary>
    /// Gets the vector's length.
    /// </summary>
    float Length { get; }

    /// <summary>
    /// Gets the sum of all the components of the vector.
    /// </summary>
    TValue Sum { get; }

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the component to get or set.</param>
    /// <returns>The component at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is outside the bounds of the collection.</exception>
    TValue this[int index] { get; set; }
}
