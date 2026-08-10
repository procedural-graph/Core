using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Sandbox.Services;

public readonly ref struct ServiceResolver
{
    internal JsonRpc JsonRpc { get; init; }

    internal ImmutableTypeLookup.Builder Builder { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>([NotNullWhen(true)] out T? service) where T : class
    {
        return Builder.TryGetOne(out service);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetService(Type type, [NotNullWhen(true)] out object? service)
    {
        return Builder.TryGetOne(type, out service);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining), RequiresDynamicCode("This code closes generic types at runtime.")]
    public ServiceResolution Local<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T service) where T : class
    {
        if (service is null)
        {
            return ServiceResolution.ArgumentNull(nameof(service));
        }

        if (Builder.Add(service))
        {
            JsonRpc.AddLocalRpcTarget(service, options: null);
        }

        return ServiceResolution.Ok();
    }

    [RequiresUnreferencedCode("Adds an instance of a service by runtime Type which may be trimmed.")]
    [RequiresDynamicCode("Adds an instance of a service by runtime Type which requires dynamic code.")]
    public ServiceResolution Local(Type type, object service)
    {
        if (type is null)
        {
            return ServiceResolution.ArgumentNull(nameof(type));
        }
        if (service is null)
        {
            return ServiceResolution.ArgumentNull(nameof(service));
        }
        if (!type.IsInstanceOfType(service))
        {
            return ServiceResolution.RuntimeTypeMismatch(type, nameof(service));
        }

        JsonRpc.AddLocalRpcTarget(service, options: null);
        Builder.Add(service, type);

        return ServiceResolution.Ok();
    }
}
