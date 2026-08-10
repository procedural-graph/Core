using StreamJsonRpc;
using System.IO;
using System.IO.Pipes;

namespace GameSharp.Sandbox.Generic.Unix.Mac;

internal sealed class MacGuest(
    AnonymousPipeServerStream outboundPipe, 
    AnonymousPipeServerStream inboundPipe, 
    JsonRpc jsonRpc, 
    scoped ref readonly MacCommandLineArguments args,
    Host<MacCommandLineArguments> host) : 
    UnixGuest<MacCommandLineArguments>(outboundPipe, inboundPipe, jsonRpc, "sandbox-exec", in args, host)
{
    private readonly string _sandboxProfilePath = args.SandboxProfilePath;

    protected override void OnDisposing()
    {
        base.OnDisposing();
        if (File.Exists(_sandboxProfilePath))
        {
            File.Delete(_sandboxProfilePath);
        }
    }
}
