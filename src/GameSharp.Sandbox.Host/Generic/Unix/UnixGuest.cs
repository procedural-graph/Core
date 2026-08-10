using StreamJsonRpc;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace GameSharp.Sandbox.Generic.Unix;

internal abstract class UnixGuest<TArgs> : Guest<TArgs, Process> where TArgs : struct, ICommandLineArguments
{
    private readonly ProcessStartInfo _startInfo;

    public UnixGuest(
        AnonymousPipeServerStream outboundPipe, 
        AnonymousPipeServerStream inboundPipe, 
        JsonRpc jsonRpc, 
        string fileName,
        scoped ref readonly TArgs args,
        Host<TArgs> host) : base(inboundPipe, outboundPipe, jsonRpc, host)
    {
        _startInfo = new()
        {
            FileName = fileName,
            Arguments = BuildCommandLineArguments(in args),
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    protected override Process WaitForProcessExit(out bool success)
    {
        Process process = Process.Start(_startInfo)!;
        process.WaitForExit();
        success = process.ExitCode == 0;
        return process;
    }

    protected override ProcessException CreateProcessException(Process processInfo, StringBuilder sb)
    {
        return UnixProcessException.Create(processInfo, sb);
    }
}
