using GameSharp.Sandbox.Services;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Sandbox;

public class ApplicationBuilder
{
    private readonly AnonymousPipeClientStream _outboundPipe;
    private readonly AnonymousPipeClientStream _inboundPipe;
    private readonly JsonRpc _jsonRpc;

    public ServiceCollectionBuilder Services { get; }

    [RequiresUnreferencedCode("This code uses a formatter/serializer that hasn't been hardened to avoid dynamic code.")]
    [RequiresDynamicCode("This code uses a formatter/serializer that hasn't been hardened to avoid dynamic code.")]
    internal ApplicationBuilder(object[] args)
    {
        ThrowArgumentExceptionIf(args is not { Length: >= 2 }, "Expected at least two arguments.", nameof(args));
        ref object argsDataRef = ref MemoryMarshal.GetArrayDataReference(args);
        _outboundPipe = PipeFromArg(ref argsDataRef, 0, PipeDirection.Out);
        _inboundPipe = PipeFromArg(ref argsDataRef, 1, PipeDirection.In);

        _jsonRpc = new JsonRpc(_outboundPipe, _inboundPipe);
        Services = new ServiceCollectionBuilder(_jsonRpc);
    }

    public Application Build()
    {
        ImmutableTypeLookup services = Services.Build();
        return new Application(_outboundPipe, _inboundPipe, _jsonRpc, services);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AnonymousPipeClientStream PipeFromArg(ref object argsDataRef, int index, PipeDirection direction)
    {
        if (Unsafe.Add(ref argsDataRef, index) is string pipeName)
        {
            return new AnonymousPipeClientStream(direction, pipeName);
        }

        ThrowArgumentException($"The element at {index} must be a valid string.", "args");
        return default!;
    }

    [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowArgumentExceptionIf([DoesNotReturnIf(true)] bool condition, string? message, string? paramName)
    {
        if (condition)
        {
            ThrowArgumentException(message, paramName);
        }
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentException(string? message, string? parameterName)
    {
        throw new ArgumentException(message, parameterName);
    }
}
