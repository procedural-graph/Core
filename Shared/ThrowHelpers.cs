using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph;

#if NET6_0_OR_GREATER
[StackTraceHidden]
#endif
internal static class ThrowHelpers
{
#if !NET7_0_OR_GREATER
    private const string NonNegativeIntegerMessage = "Must be a non-negative integer.";
#endif

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDisposed<T>([DoesNotReturnIf(true)] bool condition, T obj) where T : notnull
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(condition, obj);
#else
        if (condition)
        {
            ThrowObjectDisposedException(obj);
        }
#endif
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            ThrowArgumentNullException(paramName);
        }
#endif
    }

#if !NET7_0_OR_GREATER
    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ThrowIfNull(void* value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            ThrowArgumentNullException(paramName);
        }
    }
#endif

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string? message = null)
    {
        if (condition)
        {
            ThrowInvalidOperationException(message);
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfRange(int index, int max, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (unchecked((uint)index) >= max)
        {
            ThrowArgumentOutOfRangeException(paramName, index, $"Must be a non-negative integer less than {max}.");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfRange(int index, int min, int max, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (index < min || index >= max)
        {
            ThrowArgumentOutOfRangeException(paramName, index, $"Must be an integer in the range [{min}, {max}].");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfRange(long index, long max, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (unchecked((ulong)index) >= unchecked((ulong)max))
        {
            ThrowArgumentOutOfRangeException(paramName, index, $"Must be a non-negative integer less than {max}.");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfRange(long index, long min, long max, [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (index < min || index >= max)
        {
            ThrowArgumentOutOfRangeException(paramName, index, $"Must be an integer in the range [{min}, {max}].");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(T? value, T? comparand, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!EqualityComparer<T?>.Default.Equals(comparand, value))
        {
            ThrowArgumentOutOfRangeException(paramName, value, $"Must be equal to {comparand}.");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEqual<T>(T? value, T? comparand, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (EqualityComparer<T?>.Default.Equals(comparand, value))
        {
            ThrowArgumentOutOfRangeException(paramName, value, $"Must not be equal to {comparand}.");
        }
    }

    [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET7_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#else
        if (value < 0L)
        {
            ThrowArgumentOutOfRangeException(paramName, value, NonNegativeIntegerMessage);
        }
#endif
    }

    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET7_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#else
        if (value < 0)
        {
            ThrowArgumentOutOfRangeException(paramName, value, NonNegativeIntegerMessage);
        }
#endif
    }  

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperationException(string? message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentOutOfRangeException(string? paramName, object? actualValue, string? message)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }

#if !NET7_0_OR_GREATER
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentNullException(string? paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowObjectDisposedException(object? obj)
    {
        throw new ObjectDisposedException(obj?.GetType().FullName);
    }
#endif
}
