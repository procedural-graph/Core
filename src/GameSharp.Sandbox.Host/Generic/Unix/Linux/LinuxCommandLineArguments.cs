using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameSharp.Sandbox.Generic.Unix.Linux;

internal struct LinuxCommandLineArguments : ICommandLineArguments
{
    public string? RuntimePath { get; set; }
    public string AssemblyPath { get; set; }
    public string OutboundPipeHandle { get; set; }
    public string InboundPipeHandle { get; set; }

    public readonly string ToString(StringBuilder sb)
    {
        sb.Append("--unshare-all ");           // Isolates IPC, PID, network, and user namespaces.
        sb.Append("--die-with-parent ");       // Mirrors Windows JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE.
        sb.Append("--ro-bind / / ");           // Provides read-only access to the system (needed for .NET runtime to load).
        sb.Append("--dev /dev --proc /proc "); // Required pseudo-filesystems for .NET threading/crypto.

        sb.Append("--bind ");                  // Grants read/write access exclusively to the assembly's physical directory.
        ReadOnlySpan<char> directoryName = Path.GetDirectoryName(AssemblyPath.AsSpan());
        sb.AppendPath(directoryName);
        sb.Append(' ');
        sb.AppendPath(directoryName);

        sb.Append(" -- ");

        if (RuntimePath is { })
        {
            sb.AppendPath(RuntimePath);
            sb.Append(' ');
        }
        sb.AppendPath(AssemblyPath);

        sb.Append(' ');
        sb.Append(OutboundPipeHandle);
        sb.Append(' ');
        sb.Append(InboundPipeHandle);

        return sb.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly string ToString() => ToString(new StringBuilder());
}