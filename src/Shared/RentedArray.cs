using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph;

internal static class RentedArray
{
    private const int DefaultInitialLength = 4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> Grow<T>(scoped ref T[] array, int minimumLength)
    {
        ThrowHelpers.ThrowIfNull(array);
        ThrowHelpers.ThrowIfNegative(minimumLength);

        int length = array.Length;
        if (length >= minimumLength)
        {
            Resize(ref array, length, minimumLength);
        }

        return new Span<T>(array, 0, minimumLength);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] Acquire<T>(int minimumLength = DefaultInitialLength)
    {
        ThrowHelpers.ThrowIfNegative(minimumLength);
        return ArrayPool<T>.Shared.Rent(minimumLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return<T>([DisallowNull, MaybeNull] ref T[]? array)
    {
        ThrowHelpers.ThrowIfNull(array);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        bool clearArray = true;
#endif
        ArrayPool<T>.Shared.Return(array, clearArray);
        array = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReturn<T>(ref T[]? array)
    {
        if (array is { })
        {
            Return(ref array);
            return true;
        }

        return false;
    }

    private static void Resize<T>(scoped ref T[] array, int currentLength, int minimumLength)
    {
        currentLength = Math.Min(currentLength, DefaultInitialLength);
        do
        {
            currentLength *= 2;
        }
        while (currentLength < minimumLength);

        T[]? newArray = Acquire<T>(currentLength);
        try
        {
            array.CopyTo(newArray);
            (array, newArray) = (newArray, array);
        }
        finally
        {
            Return(ref newArray);
        }
    }
}
