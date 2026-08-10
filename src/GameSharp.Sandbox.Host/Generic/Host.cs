using StreamJsonRpc;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace GameSharp.Sandbox.Generic;

internal abstract class Host<TArgs>(
    IJsonRpcFactory factory, 
    FrozenDictionary<string, RuntimeHostedProcessFactory> runtimeProcessFactories, 
    ImmutableArray<ProcessFactory> processFactories) : Host 
    where TArgs : struct, ICommandLineArguments
{
    private readonly FrozenDictionary<string, RuntimeHostedProcessFactory> _runtimeProcessFactories = runtimeProcessFactories;
    private readonly ImmutableArray<ProcessFactory> _processFactories = processFactories;

    public override Task<CancellationTokenRegistration> ExecuteAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        const string FileDoesNotExistMessage = "The file at the specified path does not exist.";
        const string FileIsNotSupportedMessage = "The file at the specified path is not a supported assembly or executable.";

#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(Disposed, this);
#else
        if (Disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif

        if (!File.Exists(assemblyPath))
        {
#if NET6_0_OR_GREATER
            ThrowHelpers.ThrowArgumentException(FileDoesNotExistMessage, nameof(assemblyPath));
            return default;
#else
            throw new ArgumentException(FileDoesNotExistMessage, nameof(assemblyPath));
#endif
        }

        TArgs args = CreateDefault();
        InheritableAnonymousPipe(PipeDirection.Out, out AnonymousPipeServerStream outboundPipe, out string outboundHandle);
        args.OutboundPipeHandle = outboundHandle;
        try
        {
            InheritableAnonymousPipe(PipeDirection.In, out AnonymousPipeServerStream inboundPipe, out string inboundHandle);
            args.InboundPipeHandle = inboundHandle;
            JsonRpc jsonRpc = new(outboundPipe, inboundPipe);
            try
            {
                if (TryGetNativeProcessFactory(assemblyPath, ref args, out ProcessFactory? nativeProcessFactory))
                {
                    return LaunchAndExecuteInstance(ref args, outboundPipe, inboundPipe, jsonRpc, cancellationToken);
                }
                if (TryGetRuntimeHostedProcessFactory(assemblyPath, out RuntimeHostedProcessFactory? processFactory))
                {
                    return LaunchRuntimeHostedProcess(assemblyPath, ref args, outboundPipe, inboundPipe, jsonRpc, processFactory, cancellationToken);
                }
#if NET6_0_OR_GREATER
                ThrowHelpers.ThrowArgumentException(FileIsNotSupportedMessage, nameof(assemblyPath));
                return default;
#else
                throw new ArgumentException(FileIsNotSupportedMessage, nameof(assemblyPath));
#endif
            }
            catch
            {
                jsonRpc.Dispose();
                inboundPipe.Dispose();
                throw;
            }
        }
        catch
        {
            outboundPipe.Dispose();
            throw;
        }
    }

    private Task<CancellationTokenRegistration> LaunchRuntimeHostedProcess(string assemblyPath, ref TArgs args, AnonymousPipeServerStream outboundPipe, 
        AnonymousPipeServerStream inboundPipe, JsonRpc jsonRpc, RuntimeHostedProcessFactory processFactory, CancellationToken cancellationToken)
    {
        const string BadFormatMessage = "The assembly at the specified path is not a valid executable format.";

        if (processFactory.TryConfigure(assemblyPath, ref args))
        {
            return LaunchAndExecuteInstance(ref args, outboundPipe, inboundPipe, jsonRpc, cancellationToken);
        }

        if (processFactory.RuntimeFullName is null)
        {
            string message = $"The assembly at the specified path requires {processFactory.RuntimeDisplayName}, which is not installed on this system.";
#if NET6_0_OR_GREATER
            ThrowHelpers.ThrowArgumentException(message, nameof(assemblyPath));
            return default;
#else
            throw new ArgumentException(message, nameof(assemblyPath));
#endif
        }

#if NET6_0_OR_GREATER
        ThrowHelpers.ThrowArgumentException(BadFormatMessage, nameof(assemblyPath));
        return default;
#else
        throw new ArgumentException(BadFormatMessage, nameof(assemblyPath));
#endif
    }

    protected abstract Guest Launch(JsonRpc jsonRpc, AnonymousPipeServerStream outboundPipe, AnonymousPipeServerStream inboundPipe, ref TArgs args);

    protected virtual TArgs CreateDefault()
    {
        return default;
    }

    private bool TryGetNativeProcessFactory(
#if NETFRAMEWORK
#nullable disable
        string assemblyPath, ref TArgs args, out ProcessFactory processFactory)
#else
        string assemblyPath, ref TArgs args, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ProcessFactory? processFactory)
#endif
    {
        foreach (ProcessFactory factory in _processFactories)
        {
            if (factory.TryConfigure(assemblyPath, ref args))
            {
                processFactory = factory;
                return true;
            }
        }

        processFactory = null;
        return false;
#nullable restore
    }

    private bool TryGetRuntimeHostedProcessFactory(
#if NETFRAMEWORK
#nullable disable
        string assemblyPath, out RuntimeHostedProcessFactory processFactory)
    {
        if (Path.GetExtension(assemblyPath) is not { Length: > 0 } extension)
#else
        ReadOnlySpan<char> assemblyPath, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RuntimeHostedProcessFactory? processFactory)
    {
        if (Path.GetExtension(assemblyPath) is not { IsEmpty: false } extension)
#endif
        {
            processFactory = null;
            return false;
        }

#if NET9_0_OR_GREATER
        var altLookup = _runtimeProcessFactories.GetAlternateLookup<ReadOnlySpan<char>>();
        if (altLookup.TryGetValue(extension, out processFactory))
#elif NETFRAMEWORK
        if (_runtimeProcessFactories.TryGetValue(extension, out processFactory))
#else
        if (_runtimeProcessFactories.TryGetValue(extension.ToString(), out processFactory))
#endif
        {
            return true;
        }

        processFactory = null;
        return false;
#nullable restore
    }

    private Task<CancellationTokenRegistration> LaunchAndExecuteInstance(ref TArgs args, AnonymousPipeServerStream outboundPipe, 
        AnonymousPipeServerStream inboundPipe, JsonRpc jsonRpc, CancellationToken cancellationToken)
    {
        factory.Configure(jsonRpc);
        Guest instance = Launch(jsonRpc, outboundPipe, inboundPipe, ref args);
        instance.Lifetime.ContinueWith(RemoveInstance, instance, StoppingToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return LaunchInstanceAsync(instance, cancellationToken);
    }

    private async Task<CancellationTokenRegistration> LaunchInstanceAsync(Guest instance, CancellationToken cancellationToken)
    {
        CancellationTokenRegistration reg = await instance.StartAsync(cancellationToken).ConfigureAwait(false);
        AddInstance(instance);
        return reg;
    }

    private void RemoveInstance(Task task, Guest instance) => RemoveInstance(instance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InheritableAnonymousPipe(PipeDirection direction, out AnonymousPipeServerStream pipe, out string clientHandle)
    {
        pipe = new(direction, HandleInheritability.Inheritable);
        clientHandle = pipe.GetClientHandleAsString();
        pipe.DisposeLocalCopyOfClientHandle();
    }
}
