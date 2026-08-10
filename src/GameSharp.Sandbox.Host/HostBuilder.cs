using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = object;
#endif

namespace GameSharp.Sandbox;

public sealed class HostBuilder
{
    private ref struct LockHandle
    {
        private bool _held;
        private readonly Lock _lock;

        public LockHandle(Lock @lock)
        {
            _lock = @lock;
#if NET9_0_OR_GREATER
            _lock.Enter();
#else
            System.Threading.Monitor.Enter(_lock);
#endif
            _held = true;
        }

        public void Dispose()
        {
            if (_held)
            {
#if NET9_0_OR_GREATER
                _lock.Exit();
#else
                System.Threading.Monitor.Exit(_lock);
#endif
            }

            _held = false;
        }
    }

    private struct FactoryResolver
    {
        public bool isRuntimeHosted;
        public Delegate? factory;
        public object? state;
    }

    private readonly List<FactoryResolver> _processFactoryFactories;
    private readonly Dictionary<string, RuntimeHostedProcessFactory?> _runtimeHostedFactories;
    private readonly Lock _syncRoot = new();
    private readonly IJsonRpcFactory _jsonRpcFactory;
    private volatile bool _hostCreated;

    internal HostBuilder(IJsonRpcFactory jsonRpcFactory)
    {
        _processFactoryFactories = [];
        _runtimeHostedFactories = new(StringComparer.OrdinalIgnoreCase);
        _jsonRpcFactory = jsonRpcFactory;
    }

    public HostBuilder AddProcessFactory<TFactory, TState>(Func<TState, TFactory> factory, TState state)
        where TFactory : ProcessFactory
        where TState : class
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(state);
#else
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }
#endif
        using LockHandle handle = AquireLock();

        FactoryResolver resolver = new()
        {
            factory = factory,
            state = state
        };

        _processFactoryFactories.Add(resolver);

        return this;
    }

    public HostBuilder AddProcessFactory<TFactory>(Func<TFactory> factory) where TFactory : ProcessFactory
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(factory);
#else
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
#endif
        using LockHandle handle = AquireLock();

        FactoryResolver resolver = new() { factory = factory };

        _processFactoryFactories.Add(resolver);

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HostBuilder AddProcessFactory<TFactory>() where TFactory : ProcessFactory, new()
    {
        return AddProcessFactory(static () => new TFactory());
    }

    public HostBuilder AddProcessFactoryProvider(RuntimeHostedProcessFactoryProvider provider)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(provider);
#else
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }
#endif

        using LockHandle handle = AquireLock();

#if NETFRAMEWORK
        if (!_runtimeHostedFactories.ContainsKey(provider.AssemblyFileExtension))
        {
            _runtimeHostedFactories.Add(provider.AssemblyFileExtension, null);
        }
        else
#else
        if (!_runtimeHostedFactories.TryAdd(provider.AssemblyFileExtension, null))
#endif
        {
            string message = $"A provider for assemblies with the extension: '{provider.AssemblyFileExtension}' has already been registered.";
#if NET6_0_OR_GREATER
            ThrowHelpers.ThrowArgumentException(message, nameof(provider));
#else
            throw new ArgumentException(message, nameof(provider)); 
#endif
        }

        FactoryResolver resolver = new()
        {
            isRuntimeHosted = true,
            state = provider
        };

        _processFactoryFactories.Add(resolver);

        return this;
    }

    public Host Build()
    {
        const string InvalidResolverConfigurationMessage = "Invalid resolver configuration";

        using LockHandle handle = AquireLock();
        _hostCreated = true;

        Span<string> directoryNames = new(Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator));
        ref string start = ref MemoryMarshal.GetReference(directoryNames);
        ref string end = ref Unsafe.Add(ref start, directoryNames.Length);

        ImmutableArray<ProcessFactory>.Builder processFactories = ImmutableArray.CreateBuilder<ProcessFactory>();

        foreach (FactoryResolver resolver in _processFactoryFactories)
        {
            switch (resolver)
            {
                case { isRuntimeHosted: false, state: null }:
                    processFactories.Add(Unsafe.As<Func<ProcessFactory>>(resolver.factory)!());
                    break;
                case { isRuntimeHosted: false, state: { } }:
                    processFactories.Add(Unsafe.As<Func<object, ProcessFactory>>(resolver.factory)!(resolver.state));
                    break;
                case { isRuntimeHosted: true, state: RuntimeHostedProcessFactoryProvider provider }:
                    _runtimeHostedFactories[provider.AssemblyFileExtension] = provider.Create(ref start, ref end);
                    break;
#if NET6_0_OR_GREATER
                default:
                    ThrowHelpers.ThrowInvalidOperationException(InvalidResolverConfigurationMessage);
                    break;
#else
                    default: 
                        throw new InvalidOperationException(InvalidResolverConfigurationMessage);
#endif
            }
        }
        _processFactoryFactories.Clear();

        FrozenDictionary<string, RuntimeHostedProcessFactory> runtimeHostedFactories = _runtimeHostedFactories.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)!;
        _runtimeHostedFactories.Clear();

#if NETFRAMEWORK
        return new Generic.Windows.WindowsHost(_jsonRpcFactory, runtimeHostedFactories, processFactories.ToImmutable());
#else
        if (Platform.IsWindows())
        {
            return new Generic.Windows.WindowsHost(_jsonRpcFactory, runtimeHostedFactories, processFactories.ToImmutable());
        }
        else if (Platform.IsMacOs())
        {
            return new Generic.Unix.Mac.MacHost(_jsonRpcFactory, runtimeHostedFactories, processFactories.ToImmutable());
        }
        else if (Platform.IsLinux())
        {
            return new Generic.Unix.Linux.LinuxHost(_jsonRpcFactory, runtimeHostedFactories, processFactories.ToImmutable());
        }
        else
        {
#if NET6_0_OR_GREATER
            ThrowHelpers.UnsupportedPlatform();
            return default!;
#else
            throw new PlatformNotSupportedException();
#endif
        }
#endif
    }

    private LockHandle AquireLock()
    {
        const string HostCreatedMessage = "The host has already been built.";

        if (_hostCreated)
        {
#if NET6_0_OR_GREATER
            ThrowHelpers.ThrowInvalidOperationException(HostCreatedMessage);
#else
            throw new InvalidOperationException(HostCreatedMessage);
#endif
        }

        LockHandle handle = new(_syncRoot);

        try
        {
            if (_hostCreated)
            {
#if NET6_0_OR_GREATER
                ThrowHelpers.ThrowInvalidOperationException(HostCreatedMessage);
#else
                throw new InvalidOperationException(HostCreatedMessage);
#endif
            }

            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }
}
