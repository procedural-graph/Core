using Microsoft.Extensions.Logging;
using SandboxEscape;
using StreamJsonRpc;
using System.Threading.Tasks;

namespace GameSharp.Sandbox.Tests;

internal sealed partial class JsonRpcFactory(ILogger local, ILogger remote) : IJsonRpcFactory
{
    private readonly TaskCompletionSource<ISandboxTest> _tcs = new();

    public void Configure(JsonRpc jsonRpc)
    {
        LogConfiguringRemoteJsonRpcTarget(local);
        jsonRpc.AddLocalRpcTarget(remote);
        _tcs.TrySetResult(jsonRpc.Attach<ISandboxTest>());
    }

    public Task<ISandboxTest> GetSandboxTestAsync()
    {
        return _tcs.Task;
    }

    [LoggerMessage(EventId = 400, Level = LogLevel.Information, Message = "Configuring remote JSON-RPC target.")]
    private static partial void LogConfiguringRemoteJsonRpcTarget(ILogger logger);
}
