namespace ProceduralGraph.Collections;

/// <summary>
/// Defines an operation that applies a transformation to a specified 1D region (e.g., a row or a column) 
/// of a data buffer containing <typeparamref name="T"/> elements.
/// </summary>
/// <typeparam name="T">Specifies the type of elements in the data buffer. Must be an unmanaged type.</typeparam>
public unsafe interface IMapOperation<T> where T : unmanaged
{
    /// <summary>
    /// Applies a transformation to a specified region of the data buffer.
    /// </summary>
    /// <param name="source">A pointer to the start of the data buffer region to be transformed. Must not be <see langword="null"/>.</param>
    /// <param name="index">
    /// The index (e.g., row or column index) within the data buffer where the transformation occurs. 
    /// Must be within the bounds of the map.
    /// </param>
    /// <param name="length">The number of elements in the region to be transformed. Must be greater than zero.</param>
    void Apply(T* source, long index, long length);
}

/// <summary>
/// Defines an operation that applies a transformation to a specified 1D region of a source data buffer 
/// and writes the result to a destination buffer.
/// </summary>
/// <typeparam name="TSource">Specifies the type of elements in the source buffer. Must be an unmanaged type.</typeparam>
/// <typeparam name="TResult">Specifies the type of elements in the destination buffer. Must be an unmanaged type.</typeparam>
public unsafe interface IMapOperation<TSource, TResult> where TSource : unmanaged where TResult : unmanaged
{
    /// <summary>
    /// Applies a transformation to the source data from the input source and writes the result to the specified destination location.
    /// </summary>
    /// <param name="source">
    /// A pointer to the source <typeparamref name="TSource"/> region to be used in the transformation. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="destination">
    /// A pointer to the destination buffer of <typeparamref name="TResult"/> where the transformation result will be written. 
    /// Must not be <see langword="null"/> and must have sufficient capacity.
    /// </param>
    /// <param name="index">
    /// The index (e.g., row or column index) within the destination buffer where the transformation result will be applied.
    /// </param>
    /// <param name="length">
    /// The number of elements to be transformed and written to the destination. Must be positive.
    /// </param>
    void Apply(TSource* source, TResult* destination, long index, long length);
}

/// <summary>
/// Defines an operation that applies a transformation to data from two unmanaged source types 
/// and writes the result to a specified destination.
/// </summary>
/// <typeparam name="TSource1">The type of the first source data, which must be unmanaged.</typeparam>
/// <typeparam name="TSource2">The type of the second source data, which must be unmanaged.</typeparam>
/// <typeparam name="TResult">The type of the result data, which must be unmanaged.</typeparam>
public unsafe interface IMapOperation<TSource1, TSource2, TResult>
    where TSource1 : unmanaged
    where TSource2 : unmanaged
    where TResult : unmanaged
{
    /// <summary>
    /// Applies a transformation to the source data from two input sources and writes the result to the specified destination location.
    /// </summary>
    /// <param name="source1">A pointer to the first source <typeparamref name="TSource1"/> region. Must not be <see langword="null"/>.</param>
    /// <param name="source2">A pointer to the second source <typeparamref name="TSource2"/> region. Must not be <see langword="null"/>.</param>
    /// <param name="destination">
    /// A pointer to the destination buffer of <typeparamref name="TResult"/> where the transformation result will be written. 
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="index">The index (e.g., row or column index) within the buffer where the transformation result will be applied.</param>
    /// <param name="length">The number of elements to be transformed and written to the destination.</param>
    void Apply(TSource1* source1, TSource2* source2, TResult* destination, long index, long length);
}