using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Collections;

/// <summary>
/// Provides utility methods for renting, growing, and returning arrays using the shared array pool.
/// </summary>
public static class RentedArray
{
    private const int DefaultInitialLength = 4;

    /// <summary>
    /// Ensures that the specified array has a length greater than or equal to the specified minimum length. 
    /// If the array is too small, a new array is rented from the <seealso cref="ArrayPool{T}.Shared">shared array pool</seealso>, 
    /// the contents of the original array are copied to the new array, and the original array is returned to the pool.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">A reference to the array to expand.</param>
    /// <param name="minimumLength">The minimum number of elements the array should be able to store. Must be non-negative.</param>
    /// <returns>A span over the array with the specified minimum length.</returns>
    public static Span<T> Grow<T>(scoped ref T[] array, int minimumLength)
    {
        ThrowHelpers.ThrowIfNull(array);
        ThrowHelpers.ThrowIfNegative(minimumLength);

        int newLength = array.Length;

        if (newLength >= minimumLength)
        {
            return new Span<T>(array, 0, minimumLength);
        }

        newLength = newLength == 0 ? DefaultInitialLength : newLength;
        do
        {
            newLength *= 2;
        }
        while (newLength < minimumLength);

        T[]? newArray = Acquire<T>(newLength);
        try
        {
            array.CopyTo(newArray);
            (array, newArray) = (newArray, array);
            return new Span<T>(array, 0, minimumLength);
        }
        finally
        {
            Return(ref newArray);
        }
    }

    /// <returns>A new array containing the elements of the specified collection.</returns>
    /// <inheritdoc cref="Copy{T}(ICollection{T}, out T[])"/>
    public static T[] Copy<T>(ICollection<T> source)
    {
        ThrowHelpers.ThrowIfNull(source);
        T[]? array = Acquire<T>(source.Count);
        try
        {
            source.CopyTo(array, 0);
            return array;
        }
        catch
        {
            Return(ref array);
            throw;
        }
    }

    /// <summary>
    /// Creates a copy of the specified collection in a new array rented from the <seealso cref="ArrayPool{T}.Shared">shared array pool</seealso>.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="source">The collection to copy. Must not be <see langword="null"/>.</param>
    /// <param name="array">
    /// When this method returns, contains the array that was rented from the pool and used to store the elements of the collection. 
    /// The caller is responsible for returning this array to the pool when it is no longer needed.
    /// </param>
    /// <returns>A new span with the same length as the collection over <paramref name="array"/>.</returns>
    public static Span<T> Copy<T>(ICollection<T> source, [NotNull] out T[]? array)
    {
        ThrowHelpers.ThrowIfNull(source);
        int count = source.Count;
        array = Acquire<T>(count);
        try
        {
            source.CopyTo(array, 0);
            return array.AsSpan(0, count);
        }
        catch
        {
            Return(ref array);
            throw;
        }
    }

    /// <summary>
    /// Rents an array of the specified type and minimum length from the <seealso cref="ArrayPool{T}.Shared">shared array pool</seealso>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array to rent.</typeparam>
    /// <param name="minimumLength">The minimum number of elements the returned array should be able to store. Must be non-negative.</param>
    /// <returns>An array of type <typeparamref name="T"/> with a length greater than or equal to <paramref name="minimumLength"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Acquire<T>(int minimumLength = DefaultInitialLength)
    {
        ThrowHelpers.ThrowIfNegative(minimumLength);
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    /// <summary>
    /// Returns the specified array to the <seealso cref="ArrayPool{T}.Shared">shared array pool</seealso> and sets the reference to null.
    /// </summary>
    /// <remarks>
    /// <list type="termdef">
    /// <item>
    /// <term>.NET Framework</term>
    /// <description>Always clears the array before returning it to the pool.</description>
    /// </item>
    /// <item>
    /// <term>.NET Core &amp; .NET Standard</term>
    /// <description>
    /// When <typeparamref name="T"/> is a reference type or contains references, the array is cleared before returning it to the pool. 
    /// Otherwise, the array is not cleared.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">A reference to the array to return. The reference will be set to null after the array is returned to the pool.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return<T>([DisallowNull, MaybeNull] ref T[]? array)
    {
        ThrowHelpers.ThrowIfNull(array);
#if NETFRAMEWORK
        bool clearArray = true;
#else
        bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#endif
        ArrayPool<T>.Shared.Return(array, clearArray);
        array = null;
    }
}
