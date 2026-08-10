using System;
using System.IO;
using System.Text;

namespace GameSharp.Sandbox;

public abstract class ProcessException : Exception
{
    public abstract ReadOnlySpan<char> ProcessName { get; }

    public int ProcessID { get; }

    public long ExitCode { get; }

    internal ProcessException(int processID, long exitCode, StreamReader stdError, StringBuilder sb) : base(CreateMessage(processID, exitCode, stdError, sb))
    {
        ProcessID = processID;
        ExitCode = exitCode;
    }

    private static string CreateMessage(int processID, long exitCode, StreamReader stdError, StringBuilder sb)
    {
        sb.Append(" (");
        sb.Append(processID);
        sb.Append(") exited with code ");
        sb.Append(exitCode);
        sb.Append('.');
        int curr = stdError.Read();
        if (curr != -1)
        {
            sb.AppendLine();
            sb.Append((char)curr);
            for (; (curr = stdError.Read()) != -1; sb.Append((char)curr)) ;
        }
        return sb.ToString();
    }
}
