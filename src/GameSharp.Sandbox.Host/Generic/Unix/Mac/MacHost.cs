using StreamJsonRpc;
using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameSharp.Sandbox.Generic.Unix.Mac;

internal sealed class MacHost(IJsonRpcFactory factory,
    FrozenDictionary<string, RuntimeHostedProcessFactory> runtimeProcessFactories,
    ImmutableArray<ProcessFactory> processFactories) : Host<MacCommandLineArguments>(factory, runtimeProcessFactories, processFactories)
{
    protected override Guest Launch(JsonRpc jsonRpc, AnonymousPipeServerStream outboundPipe, AnonymousPipeServerStream inboundPipe, ref MacCommandLineArguments args)
    {
        args.SandboxProfilePath = Path.GetTempFileName();
        using (FileStream stream = new(args.SandboxProfilePath, FileMode.Open, FileAccess.Write))
        {
            stream.Write("(version 1)\n(deny default)\n(allow file-read*)\n(allow file-write* (subpath \""u8);
            ReadOnlySpan<char> pathToAssembly = Path.GetDirectoryName(args.AssemblyPath.AsSpan());
            WriteEncodedPath(stream, pathToAssembly);
            stream.Write("\")\n   (regex #\"^/private/tmp/\")\n   (regex #\"^/var/folders/\"))\n(allow process-exec)\n(allow mach-lookup)\n(allow ipc-posix-shm)"u8);
        }
        return new MacGuest(outboundPipe, inboundPipe, jsonRpc, in args, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteEncodedPath(FileStream stream, ReadOnlySpan<char> pathToAssembly)
    {
        int maxByteLength = Encoding.UTF8.GetMaxByteCount(pathToAssembly.Length);
        if (maxByteLength <= 1024)
        {
            Span<byte> bytes = stackalloc byte[maxByteLength];
            int encodedCount = Encoding.UTF8.GetBytes(pathToAssembly, bytes);
            stream.Write(bytes[..encodedCount]);
            return;
        }
        WriteEncodedPathFromRentedArray(stream, pathToAssembly, maxByteLength);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteEncodedPathFromRentedArray(FileStream stream, ReadOnlySpan<char> pathToAssembly, int maxByteLength)
    {
        ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;
        byte[] bytes = arrayPool.Rent(maxByteLength);
        try
        {
            int encodedCount = Encoding.UTF8.GetBytes(pathToAssembly, bytes);
            stream.Write(bytes, 0, encodedCount);
        }
        finally
        {
            arrayPool.Return(bytes, clearArray: false);
        }
    }
}
