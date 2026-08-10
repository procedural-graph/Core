using System;
using System.IO;
using System.Text;

namespace GameSharp.Sandbox.Generic.Windows;

internal sealed class WindowsProcessException : ProcessException
{
#if NETFRAMEWORK
    private readonly string _executableName;
    public override ReadOnlySpan<char> ProcessName => _executableName.AsSpan();

    private WindowsProcessException(string executableName, int processID, long exitCode, StreamReader stdError, StringBuilder sb)
        : base(processID, exitCode, stdError, sb)
    {
        _executableName = executableName;
    }

    public static WindowsProcessException Create(string executablePath, int processID, long exitCode, StreamReader stdError, StringBuilder sb)
    {
        string executableName = Path.GetFileNameWithoutExtension(executablePath);
        sb.Append(executableName);
        return new WindowsProcessException(executableName, processID, exitCode, stdError, sb);
    }
#else
    private readonly string _executablePath;
    public override ReadOnlySpan<char> ProcessName => Path.GetFileNameWithoutExtension(_executablePath.AsSpan());

    private WindowsProcessException(string executablePath, int processID, long exitCode, StreamReader stdError, StringBuilder sb)
        : base(processID, exitCode, stdError, sb)
    {
        _executablePath = executablePath;
    }

    public static WindowsProcessException Create(string executablePath, int processID, long exitCode, StreamReader stdError, StringBuilder sb)
    {
        sb.Append(Path.GetFileNameWithoutExtension(executablePath.AsSpan()));
        return new WindowsProcessException(executablePath, processID, exitCode, stdError, sb);
    }
#endif
}
