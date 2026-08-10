using StreamJsonRpc;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Pipes;

namespace GameSharp.Sandbox.Generic.Unix.Linux;

internal sealed class LinuxHost(IJsonRpcFactory factory,
    FrozenDictionary<string, RuntimeHostedProcessFactory> runtimeProcessFactories,
    ImmutableArray<ProcessFactory> processFactories) : Host<LinuxCommandLineArguments>(factory, runtimeProcessFactories, processFactories)
{
    protected override Guest Launch(JsonRpc jsonRpc, AnonymousPipeServerStream outboundPipe, AnonymousPipeServerStream inboundPipe, ref LinuxCommandLineArguments args)
    {
        return new LinuxGuest(outboundPipe, inboundPipe, jsonRpc, in args, this);
    } 
}
