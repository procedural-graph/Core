using System.Runtime.CompilerServices;
using System.Text;

namespace GameSharp.Sandbox.Generic.Unix.Mac;

internal struct MacCommandLineArguments : ICommandLineArguments
{
    public string? RuntimePath { get; set; }

    public string AssemblyPath { get; set; }

    public string OutboundPipeHandle { get; set; }

    public string InboundPipeHandle { get; set; }

    public string SandboxProfilePath { get; set; }

    public readonly string ToString(StringBuilder sb)
    {
        sb.Append("-f ");
        sb.AppendPath(SandboxProfilePath);
        sb.Append(' ');
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
