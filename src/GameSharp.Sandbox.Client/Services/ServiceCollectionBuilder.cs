using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Sandbox.Services;

public sealed class ServiceCollectionBuilder : ICollection<Type>
{
    public delegate ServiceResolution StatelessFactory(Type type, ref readonly ServiceResolver resolver);
    public delegate ServiceResolution StatefulFactory(Type type, ref readonly ServiceResolver resolver, object state);

    public struct Enumerator : IEnumerator<Type>
    {
        private List<ServiceDescriptor>.Enumerator _enumerator;
        public Type Current => _enumerator.Current.Type;
        object IEnumerator.Current => Current;
        internal Enumerator(List<ServiceDescriptor>? services) => _enumerator = services?.GetEnumerator() ?? default;
        public bool MoveNext() => _enumerator.MoveNext();
        public void Dispose() => _enumerator.Dispose();
        readonly void IEnumerator.Reset() => ThrowUnsupportedMemberException(this);
    }

    private const string RequiresDynamicCodeMessage = "Adding services by runtime Type requires dynamic proxy generation which is not supported in Native AOT.";
    private List<ServiceDescriptor>? _services = [];
    private readonly JsonRpc _jsonRpc;
    public int Count => Volatile.Read(ref _services)?.Count ?? 0;
    bool ICollection<Type>.IsReadOnly => false;

    internal ServiceCollectionBuilder(JsonRpc jsonRpc)
    {
        _jsonRpc = jsonRpc;
    }

    [RequiresUnreferencedCode("Calls ServiceDescriptor.Remote<T>()")]
    [RequiresDynamicCode("Calls ServiceDescriptor.Remote<T>()")]
    public bool AddRemote<T>() where T : class
    {
        return Add(ServiceDescriptor.Remote<T>());
    }

    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(RequiresDynamicCodeMessage)]
    public bool AddRemote(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Add(ServiceDescriptor.Remote(type));
    }

    [RequiresDynamicCode("Calls ServiceDescriptor.Local<T>()")]
    public bool AddLocal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : class, new()
    {
        return Add(ServiceDescriptor.Local<T>());
    }

    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(RequiresDynamicCodeMessage)]
    public bool AddLocal(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Add(ServiceDescriptor.Local(type));
    }

    public bool AddLocal<T>(StatelessFactory factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Add(ServiceDescriptor.Local<T>(factory));
    }

    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public bool AddLocal(Type type, StatelessFactory factory)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(factory);
        return Add(ServiceDescriptor.Local(type, factory));
    }

    public bool AddLocal<T>(StatefulFactory factory, object state) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(state);
        return Add(ServiceDescriptor.Local<T>(factory, state));
    }

    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public bool AddLocal(Type type, StatefulFactory factory, object state)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(state);
        return Add(ServiceDescriptor.Local(type, factory, state));
    }

    public void Clear()
    {
        List<ServiceDescriptor> services = GetServiceDescriptors();
        lock (services)
        {
            EnsureNotBuilt(services);
            services.Clear();
        }
    }

    public bool Contains(Type item)
    {
        if (!TryGetServiceDescriptors(out List<ServiceDescriptor>? services))
        {
            return false;
        }

        foreach (ServiceDescriptor service in services)
        {
            if (service.Type == item)
            {
                return true;
            }
        }

        return false;
    }

    public bool Remove<T>() where T : class => Remove(typeof(T));

    public bool Remove(Type item)
    {
        List<ServiceDescriptor> services = GetServiceDescriptors();

        int i = 0;
        foreach (ServiceDescriptor service in services)
        {
            if (service.Type == item)
            {
                lock (services)
                {
                    EnsureNotBuilt(services);
                    services.RemoveAt(i);
                }
                return true;
            }
            ++i;
        }

        return false;
    }

    public Enumerator GetEnumerator()
    {
        if (TryGetServiceDescriptors(out List<ServiceDescriptor>? services))
        {
            return new Enumerator(services);
        }

        return default;
    }

    internal ImmutableTypeLookup Build()
    {
        List<ServiceDescriptor>? services = Interlocked.Exchange(ref _services, null);

        if (services is null)
        {
            BuilderHasAlreadyBeenBuilt();
        }

        if (services.Count == 0)
        {
            return [];
        }

        ServiceResolver resolver = new()
        {
            JsonRpc = _jsonRpc,
            Builder = ImmutableTypeLookup.CreateBuilder()
        };

        lock (services)
        {
            Span<ServiceDescriptor> servicesSpan = CollectionsMarshal.AsSpan(services);
            ref ServiceDescriptor current = ref MemoryMarshal.GetReference(servicesSpan), last = ref Unsafe.Add(ref current, servicesSpan.Length - 1);
            do
            {
                for (ref ServiceDescriptor mid = ref last; Unsafe.IsAddressLessThanOrEqualTo(ref current, ref mid); current = ref Unsafe.Add(ref current, 1))
                {
                    ServiceResolution resolution = current.Invoke(in resolver);
                    switch (resolution.kind)
                    {
                        case ServiceResolutionKind.Ok: break;
                        case ServiceResolutionKind.Retry: DeferServiceResolution(ref current, ref mid, resolution.value!); break;
                        case ServiceResolutionKind.Failed: FailedToInitializeService(current.Type, resolution.value as Exception); break;
                        default: KindNotKnown(resolution.kind); break;
                    }
                }
            }
            while (Unsafe.IsAddressLessThanOrEqualTo(ref current, ref last));
        }

        return resolver.Builder.ToImmutable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DeferServiceResolution(ref ServiceDescriptor current, ref ServiceDescriptor mid, object value)
    {
        (current, mid) = (mid, current with { Dependency = Cast<Type>(value) });
        mid = ref Unsafe.Subtract(ref mid, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Add(ServiceDescriptor serviceDescriptor)
    {
        List<ServiceDescriptor> services = GetServiceDescriptors();

        if (services.Contains(serviceDescriptor))
        {
            return false;
        }

        lock (services)
        {
            EnsureNotBuilt(services);
            services.Add(serviceDescriptor);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotBuilt(List<ServiceDescriptor> services)
    {
        if (!ReferenceEquals(services, Volatile.Read(ref _services)))
        {
            BuilderHasAlreadyBeenBuilt();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetServiceDescriptors([NotNullWhen(true)] out List<ServiceDescriptor>? services)
    {
        services = Volatile.Read(ref _services);
        return services is { };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private List<ServiceDescriptor> GetServiceDescriptors()
    {
        if (!TryGetServiceDescriptors(out List<ServiceDescriptor>? services))
        {
            BuilderHasAlreadyBeenBuilt();
        }

        return services;
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void BuilderHasAlreadyBeenBuilt()
    {
        throw new InvalidOperationException($"Cannot modify a {nameof(ServiceCollectionBuilder)} that has already been built.");
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnsupportedMemberException(object instance, [CallerMemberName] string? callerMemberName = null)
    {
        throw new NotSupportedException($"{callerMemberName} is not supported on {instance.GetType().FullName}.");
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void KindNotKnown(ServiceResolutionKind kind, [CallerArgumentExpression(nameof(kind))] string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName, kind, "The specified service resolution kind is not known.");
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailedToInitializeService(Type type, Exception? innerException = null)
    {
        throw new InvalidOperationException($"Failed to initialize service of type {type.FullName}.", innerException);
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ArrayIndexIsOutOfRange(object? actualValue, [CallerArgumentExpression(nameof(actualValue))] string? paramName = null)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue,
            "The number of elements in the source collection is greater than the available space from the specified index to the end of the destination array.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Cast<T>(object value)
    {
#if DEBUG
        return (T)value;
#else
        return Unsafe.As<T>(value);
#endif
    }

    void ICollection<Type>.CopyTo(Type[] array, int arrayIndex)
    {
        if ((array.Length - (uint)arrayIndex) < Count)
        {
            ArrayIndexIsOutOfRange(arrayIndex);
        }

        if (TryGetServiceDescriptors(out List<ServiceDescriptor>? services))
        {
            ref Type elemRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), arrayIndex);
            foreach (ServiceDescriptor serviceDescriptors in services)
            {
                elemRef = serviceDescriptors.Type;
                elemRef = ref Unsafe.Add(ref elemRef, 1);
            }
        }
    }

    void ICollection<Type>.Add(Type item)
    {
        throw new NotSupportedException($"{nameof(ICollection<>.Add)} is not supported on {nameof(ServiceCollectionBuilder)}.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<Type> IEnumerable<Type>.GetEnumerator()
    {
        return GetEnumerator();
    }
}
