using System;
using System.IO;
using System.IO.Compression;

namespace GameSharp.Sandbox.Java;

internal sealed class JavaProcessFactory(string? runtimeFullName) : RuntimeHostedProcessFactory(runtimeFullName)
{
    private const string MainClassKey = "Main-Class:";
    private static int OverlapLength { get; } = MainClassKey.Length - 1;

    public override string RuntimeDisplayName => "Java";

    public override bool TryConfigure<TArgs>(string assemblyPath, ref TArgs args)
    {
        return base.TryConfigure(assemblyPath, ref args) && IsExecutableJar(assemblyPath);
    }

    private static bool IsExecutableJar(string assemblyPath)
    {
        FileStream stream = new(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false);
        try
        {
            using ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.GetEntry("META-INF/MANIFEST.MF") is not ZipArchiveEntry manifest)
            {
                return false;
            }
            using StreamReader reader = new(manifest.Open());
#if NETFRAMEWORK
            char[] buffer = new char[1024];
            for (int currLen = reader.ReadBlock(buffer, 0, buffer.Length), nextLen, offset; currLen > 0; currLen = nextLen + offset)
            {
                ReadOnlySpan<char> payload = buffer.AsSpan(0, currLen);
#else
            Span<char> buffer = stackalloc char[1024];
            for (int currLen = reader.ReadBlock(buffer), nextLen, offset; currLen > 0; currLen = nextLen + offset)
            {
                ReadOnlySpan<char> payload = buffer[..currLen];
#endif
                if (payload.Contains(MainClassKey.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (currLen < buffer.Length && reader.EndOfStream)
                {
                    break;
                }
                offset = Math.Min(OverlapLength, payload.Length);
                if (offset > 0)
                {
                    payload.Slice(currLen - offset).CopyTo(buffer);
                }
#if NETFRAMEWORK
                if ((nextLen = reader.ReadBlock(buffer, offset, buffer.Length - offset)) == 0)
#else
                if ((nextLen = reader.ReadBlock(buffer[offset..])) == 0)
#endif
                {
                    break;
                }
            }

            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
