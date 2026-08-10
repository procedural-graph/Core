using StreamJsonRpc;
using System.IO.Pipes;

namespace GameSharp.Sandbox.Generic.Unix.Linux;

internal sealed class LinuxGuest(
    AnonymousPipeServerStream outboundPipe,
    AnonymousPipeServerStream inboundPipe,
    JsonRpc jsonRpc,
    scoped ref readonly LinuxCommandLineArguments args,
    LinuxHost host) : UnixGuest<LinuxCommandLineArguments>(outboundPipe, inboundPipe, jsonRpc, "bwrap", in args, host);
