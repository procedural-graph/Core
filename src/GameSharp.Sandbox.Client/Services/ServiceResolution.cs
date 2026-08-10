using System.Runtime.CompilerServices;

namespace GameSharp.Sandbox.Services;

public readonly struct ServiceResolution
{
    internal readonly ServiceResolutionKind kind;
    internal readonly object? value;

    private ServiceResolution(ServiceResolutionKind kind, object? value)
    {
        this.kind = kind;
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceResolution Ok()
    {
        return new ServiceResolution(ServiceResolutionKind.Ok, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceResolution Failed(Exception? exception = null)
    {
        return new ServiceResolution(ServiceResolutionKind.Failed, exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceResolution UnresolvedDependency(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new ServiceResolution(ServiceResolutionKind.Retry, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ServiceResolution UnresolvedDependency<T>()
    {
        return new ServiceResolution(ServiceResolutionKind.Retry, typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ServiceResolution ArgumentNull(string? paramName)
    {
        ArgumentNullException exception = new(paramName);
        return Failed(exception);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ServiceResolution RuntimeTypeMismatch(Type expected, string? paramName)
    {
        ArgumentException exception = new($"The provided service is not an instance of the specified type {expected.FullName}.", paramName);
        return Failed(exception);
    }
}
