namespace ProceduralGraph.Collections;

/// <summary>
/// Defines a contract for applying an operation to a specified location in a map using a source operand and
/// returning a result.
/// </summary>
/// <typeparam name="TSource">The type of the source operand used to influence the operation.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the operation.</typeparam>
public interface IMapOperation<TSource, TResult>
{
    /// <summary>
    /// Applies a specified operation to the coordinates at the given x and y positions using the provided operand.
    /// </summary>
    /// <param name="x">The x-coordinate of the target location where the operation is applied.</param>
    /// <param name="y">The y-coordinate of the target location where the operation is applied.</param>
    /// <param name="operand">The operand that influences or modifies the operation performed at the specified coordinates.</param>
    /// <returns>The result of applying the operation to the specified coordinates and operand.</returns>
    TResult Apply(long x, long y, in TSource operand);
}

/// <summary>
/// Defines an operation that can be applied to a specified location using two operands.
/// </summary>
/// <typeparam name="TSource1">The type of the first operand used in the operation.</typeparam>
/// <typeparam name="TSource2">The type of the second operand used in the operation.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the operation.</typeparam>
public interface IMapOperation<TSource1, TSource2, TResult>
{
    /// <summary>
    /// Applies a specified operation to the coordinates at the given x and y positions using the provided operands.
    /// </summary>
    /// <param name="x">The x-coordinate of the target location where the operation is applied.</param>
    /// <param name="y">The y-coordinate of the target location where the operation is applied.</param>
    /// <param name="operand1">The first operand that influences or modifies the operation performed at the specified coordinates.</param>
    /// <param name="operand2">The second operand that influences or modifies the operation performed at the specified coordinates.</param>
    /// <returns>The result of applying the operation to the specified coordinates and operands.</returns>
    TResult Apply(long x, long y, in TSource1 operand1, in TSource2 operand2);
}
