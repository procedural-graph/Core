using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Sandbox.Services;

internal readonly struct ServiceDescriptor : IEquatable<ServiceDescriptor>
{
    private readonly Delegate _factory;
    private readonly object? _state;
    public readonly bool _local;

    public Type Type { get; }

    public Type? Dependency { get; init; }

    private ServiceDescriptor(Type type, Delegate factory, bool local, object? state)
    {
        Type = type;
        _factory = factory;
        _local = local;
        _state = state;
    }

    public static ServiceDescriptor Query<T>(bool local) => new(typeof(T), null!, local, null);
    public static ServiceDescriptor Query(Type type, bool local) => new(type, null!, local, null);
    [RequiresUnreferencedCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()"), RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()")]
    public static ServiceDescriptor Remote<T>() where T : class => new(typeof(T), Attach<T>, false, null);
    [RequiresUnreferencedCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()"), RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()")]
    public static ServiceDescriptor Remote(Type type) => new(type, Attach, false, null);
    [RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.AddLocalRpcTarget<T>()")]
    public static ServiceDescriptor Local<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : class, new() => 
        new(typeof(T), LocalFromParameterlessConstructor<T>, true, null);
    [RequiresDynamicCode("Calls ServiceDescriptor.LocalFromParameterlessConstructor(Type, JsonRpc, ImmutableTypeLookup.Builder)")]
    [RequiresUnreferencedCode("Calls ServiceDescriptor.LocalFromParameterlessConstructor(Type, JsonRpc, ImmutableTypeLookup.Builder)")]
    public static ServiceDescriptor Local(Type type) => new(type, LocalFromParameterlessConstructor, true, null);
    public static ServiceDescriptor Local<T>(ServiceCollectionBuilder.StatelessFactory factory) where T : class => new(typeof(T), factory, true, null);
    public static ServiceDescriptor Local(Type type, ServiceCollectionBuilder.StatelessFactory factory) => new(type, factory, true, null);
    public static ServiceDescriptor Local<T>(ServiceCollectionBuilder.StatefulFactory factory, object state) where T : class => new(typeof(T), factory, true, state);
    public static ServiceDescriptor Local(Type type, ServiceCollectionBuilder.StatefulFactory factory, object state) => new(type, factory, true, state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ServiceResolution Invoke(ref readonly ServiceResolver resolver)
    {
        if (_state is null)
        {
            ServiceCollectionBuilder.StatelessFactory func = Unsafe.As<ServiceCollectionBuilder.StatelessFactory>(_factory);
            return func.Invoke(Type, in resolver);
        }
        else
        {
            ServiceCollectionBuilder.StatefulFactory func = Unsafe.As<ServiceCollectionBuilder.StatefulFactory>(_factory);
            return func.Invoke(Type, in resolver, _state);
        }
    }

    public readonly bool Equals(ServiceDescriptor other)
    {
        return EqualityComparer<Type>.Default.Equals(Type, other.Type) && _local == other._local;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is ServiceDescriptor other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Type, _local);
    }

    [RequiresUnreferencedCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()"), RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.Attach<T>()")]
    private static object Attach<T>(Type type, JsonRpc jsonRpc) where T : class => jsonRpc.Attach<T>();

    [RequiresUnreferencedCode("Calls StreamJsonRpc.JsonRpc.Attach(Type)"), RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.Attach(Type)")]
    private static object Attach(Type type, JsonRpc jsonRpc) => jsonRpc.Attach(type);

    [RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.AddLocalRpcTarget<T>(T, JsonRpcTargetOptions)")]
    private static bool LocalFromParameterlessConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        Type type, JsonRpc jsonRpc, ImmutableTypeLookup.Builder builder, out object? result) 
        where T : class, new()
    {
        try
        {
            T instance = new();
            result = instance;

            if (builder.Add(instance))
            {
                jsonRpc.AddLocalRpcTarget(instance, options: null);
            }

            return true;
        }
        catch (Exception ex)
        {
            result = ex;
            return false;
        }
    }

    [RequiresUnreferencedCode("Calls StreamJsonRpc.JsonRpc.AddLocalRpcTarget(Object, JsonRpcTargetOptions)")]
    [RequiresDynamicCode("Calls StreamJsonRpc.JsonRpc.AddLocalRpcTarget(Object, JsonRpcTargetOptions)")]
    private static bool LocalFromParameterlessConstructor(Type type, JsonRpc jsonRpc, ImmutableTypeLookup.Builder builder, out object? result)
    {
        try
        {
            result = Activator.CreateInstance(type);

            if (result is null)
            {
                result = new ArgumentException($"{type.FullName} must have a public parameterless constructor.", nameof(type));
                return false;
            }

            if (builder.Add(result, type))
            {
                jsonRpc.AddLocalRpcTarget(result, options: null);
            }

            return true;
        }
        catch (Exception ex)
        {
            result = ex;
            return false;
        }
    }
}
