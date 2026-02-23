using System;
using System.Numerics;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Defines an interface for applying operations to data vectors and scalar values at specified coordinates, supporting
    /// both SIMD and scalar processing for numerical computations.
    /// </summary>
    /// <inheritdoc/>
    public interface ISimdMapOperation<TSource, TResult> : IMapOperation<TSource, TResult> where TSource : unmanaged where TResult : unmanaged
    {
        /// <summary>
        /// Applies a specified operation to the elements of the provided vector at the given coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the element in the vector to which the operation will be applied.</param>
        /// <param name="y">The y-coordinate of the element in the vector to which the operation will be applied.</param>
        /// <param name="operand">The vector containing the source values that will be used in the operation.</param>
        /// <returns>A vector of type TResult containing the results of the applied operation at the specified coordinates.</returns>
        Vector<TResult> Apply(int x, int y, in Vector<TSource> operand);
    }

    /// <summary>
    /// Defines an interface for applying operations to data vectors and scalar values at specified coordinates, supporting
    /// both SIMD and scalar processing for numerical computations.
    /// </summary>
    /// <inheritdoc/>
    public interface ISimdMapOperation<TSource1, TSource2, TResult> : IMapOperation<TSource1, TSource2, TResult> 
        where TSource1 : unmanaged 
        where TSource2 : unmanaged 
        where TResult : unmanaged
    {
        /// <summary>
        /// Applies a specified operation to the elements of the provided vectors at the given coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the element in the vectors to which the operation will be applied.</param>
        /// <param name="y">The y-coordinate of the element in the vectors to which the operation will be applied.</param>
        /// <param name="operand1">The first vector containing source values that will be used in the operation.</param>
        /// <param name="operand2">The second vector containing source values that will be used in the operation.</param>
        /// <returns>A vector of type TResult containing the results of the applied operation at the specified coordinates.</returns>
        Vector<TResult> Apply(int x, int y, in Vector<TSource1> operand1, in Vector<TSource2> operand2);
    }
}