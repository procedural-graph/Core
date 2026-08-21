using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal static class ThrowHelpers
{
    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    public static void Throw(Exception exception)
    {
        throw exception;
    }

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayIndexIsOutOfRange<T>(int arrayIndex, T[] array, int count, [CallerArgumentExpression(nameof(arrayIndex))] string? paramName = null)
    {
        if ((array.Length - (uint)arrayIndex) < count)
        {
            ArgumentOutOfRangeException ex = new(paramName, 
                arrayIndex,
                "The number of elements in the source collection is greater than the available space from the specified index to the end of the destination array.");
            Throw(ex);
        }
    }
}
