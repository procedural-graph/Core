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
#endif

    /// <summary>
    /// Gets or sets the component at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the component to get or set.</param>
    /// <returns>The component at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is outside the bounds of the collection.</exception>
    TValue this[int index] { get; set; }
}
