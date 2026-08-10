using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace GameSharp.Sandbox.Dotnet;

internal sealed class DotnetProcessFactory(string? runtimeFullName) : RuntimeHostedProcessFactory(runtimeFullName)
{
    public override string RuntimeDisplayName => ".NET";

    public override bool TryConfigure<TArgs>(string assemblyPath, ref TArgs args)
    {
        return base.TryConfigure(assemblyPath, ref args) && IsDotnetHostedExecutable(assemblyPath);
    }

    private static bool IsDotnetHostedExecutable(string assemblyPath)
    {
        FileStream stream = new(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false);
        PEReader peReader = new(stream);
        try
        {
            return peReader.HasMetadata && peReader.PEHeaders is { CorHeader.EntryPointTokenOrRelativeVirtualAddress: not 0 };
        }
        catch (BadImageFormatException)
        {
            return false;
        }
        finally
        {
            peReader.Dispose();
            stream.Dispose();
        }
    }
}
