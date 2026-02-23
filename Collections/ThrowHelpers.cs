using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Collections;

internal static class ThrowHelpers
{
    public static ArgumentNullException CreateArgumentNullException(string parameterName) => new(parameterName);

    public static InvalidOperationException CreateInvalidOperationException(string message) => new(message);

    public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException<T>(T actualValue, string parameterName) => new(parameterName, actualValue, null);

    public static ObjectDisposedException CreateObjectDisposedException<T>(T instance) where T : notnull => new(instance.GetType().FullName);

    public static void ThrowIf<TException, TParameter>(
        [DoesNotReturnIf(true)] bool condition,
        TParameter parameter,
        Func<TParameter, TException> factory) where TException : Exception
    {
        if (condition)
        {
            throw factory?.Invoke(parameter) ?? new Exception();
        }
    }

    public static void ThrowIf<TException, TArgument>(
        [DoesNotReturnIf(true)] bool condition,
        TArgument argument,
        Func<TArgument?, string, TException> factory,
        [CallerArgumentExpression(nameof(argument))] string parameterName = "") where TException : Exception
    {
        if (condition)
        {
            throw factory?.Invoke(argument, parameterName) ?? new Exception();
        }
    }
}