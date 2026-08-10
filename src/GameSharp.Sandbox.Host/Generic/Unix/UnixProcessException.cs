using System;
using System.Diagnostics;
using System.Text;

namespace GameSharp.Sandbox.Generic.Unix;

internal sealed class UnixProcessException : ProcessException
{
    private readonly string _processName;
    public override ReadOnlySpan<char> ProcessName => _processName.AsSpan();

    private UnixProcessException(string processName, Process process, StringBuilder sb) : base(process.Id, process.ExitCode, process.StandardError, sb)
    {
        _processName = processName;
    }

    public static UnixProcessException Create(Process process, StringBuilder sb)
    {
        string processName = process.ProcessName;
        sb.Append(processName);
        return new UnixProcessException(processName, process, sb);
    }
}
